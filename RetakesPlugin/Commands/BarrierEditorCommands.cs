using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Managers;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;
using MapBarrier = RetakesPlugin.Models.Barrier;

namespace RetakesPlugin.Commands;

public class BarrierEditorCommands
{
    private readonly RetakesPlugin _plugin;
    private readonly BarrierConfigService _barrierConfigService;
    private readonly BarrierManager _barrierManager;
    private Bombsite? _editingBombsite;
    private Bombsite? _showingBarriersForBombsite;
    private Vector? _firstPoint;
    private bool _isEditingMode;

    public BarrierEditorCommands(RetakesPlugin plugin, BarrierConfigService barrierConfigService, BarrierManager barrierManager)
    {
        _plugin = plugin;
        _barrierConfigService = barrierConfigService;
        _barrierManager = barrierManager;
        _isEditingMode = false;
    }

    public bool IsEditingMode => _isEditingMode;
    public Bombsite? ShowingBarriersForBombsite => _showingBarriersForBombsite;

    public void OnCommandShowBarriers(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Usage: !showbarriers [A/B]");
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must specify a bombsite [A / B].");
            return;
        }

        _showingBarriersForBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;

        Server.ExecuteCommand("mp_warmup_pausetimer 1");
        Server.ExecuteCommand("mp_warmuptime 999999");
        Server.ExecuteCommand("mp_warmup_start");

        _plugin.AddTimer(1.0f, () =>
        {
            if (_showingBarriersForBombsite != null)
            {
                _barrierManager.SpawnBarrier((Bombsite)_showingBarriersForBombsite);
                Logger.LogInfo("Commands", $"Barriers displayed for bombsite {_showingBarriersForBombsite}");
            }
        });

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Showing barriers for bombsite {_showingBarriersForBombsite}.");
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Use !hidebarriers to exit.");

        Logger.LogInfo("Commands", $"{player!.PlayerName} is viewing barriers for bombsite {_showingBarriersForBombsite}");
    }

    public void OnCommandHideBarriers(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        _showingBarriersForBombsite = null;
        _barrierManager.RemoveBarrier();

        Server.ExecuteCommand("mp_warmup_pausetimer 0");
        Server.ExecuteCommand("mp_warmup_end");

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Exited barrier viewing mode.");
        Logger.LogInfo("Commands", $"{player!.PlayerName} exited barrier viewing mode");
    }

    public void OnCommandEditBarriers(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Usage: !editbarriers [A/B]");
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must specify a bombsite [A / B].");
            return;
        }

        _editingBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;
        _firstPoint = null;
        _isEditingMode = true;

        Server.ExecuteCommand("mp_warmup_pausetimer 1");
        Server.ExecuteCommand("mp_warmuptime 999999");
        Server.ExecuteCommand("mp_warmup_start");

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Barrier editing mode enabled for bombsite {_editingBombsite}.");
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Use ping (default: X key) to mark two points for a barrier.");
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Use !removebarrier near a barrier to delete it.");
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Use !testbarrier to test the current barriers.");
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Use !donebarriers when finished.");

        Logger.LogInfo("Commands", $"Editing barriers for bombsite {_editingBombsite} by {player!.PlayerName}");
    }

    public HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        if (!_isEditingMode || _editingBombsite == null)
        {
            return HookResult.Continue;
        }

        var player = @event.Userid;
        if (!PlayerHelper.IsValid(player))
        {
            return HookResult.Continue;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            return HookResult.Continue;
        }

        var pingPos = new Vector(@event.X, @event.Y, @event.Z);

        if (_firstPoint == null)
        {
            _firstPoint = pingPos;
            player.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} First point set. Ping the opposite corner to create the barrier.");
            Logger.LogDebug("Commands", $"First barrier point set: {_firstPoint}");
        }
        else
        {
            var newBarrier = new MapBarrier(_firstPoint, pingPos)
            {
                Bombsite = (Bombsite)_editingBombsite
            };

            var didAddBarrier = _barrierConfigService.AddBarrier(newBarrier);
            if (didAddBarrier)
            {
                _barrierManager.CalculateMapBarriers();
                player.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} Barrier added successfully!");
                Logger.LogInfo("Commands", $"{player.PlayerName} added barrier at bombsite {_editingBombsite}");
            }
            else
            {
                player.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} Error adding barrier (duplicate?)");
            }

            _firstPoint = null;
        }

        return HookResult.Continue;
    }

    public void OnCommandRemoveBarrier(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (_editingBombsite == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must be in barrier editing mode.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            return;
        }

        var barriers = _barrierManager.GetBarriers((Bombsite)_editingBombsite);

        if (barriers.Count == 0)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No barriers found.");
            return;
        }

        var playerPos = player!.PlayerPawn.Value!.AbsOrigin!;
        MapBarrier? closestBarrier = null;
        var closestDistance = double.MaxValue;

        foreach (var barrier in barriers)
        {
            var centerX = (barrier.MinPos.X + barrier.MaxPos.X) / 2;
            var centerY = (barrier.MinPos.Y + barrier.MaxPos.Y) / 2;
            var centerZ = (barrier.MinPos.Z + barrier.MaxPos.Z) / 2;

            var centerPos = new Vector(centerX, centerY, centerZ);
            var distance = GameRulesHelper.GetDistanceBetweenVectors(playerPos, centerPos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBarrier = barrier;
            }
        }

        if (closestBarrier == null || closestDistance > 500)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No barriers found within 500 units.");
            return;
        }

        var didRemoveBarrier = _barrierConfigService.RemoveBarrier(closestBarrier);
        if (didRemoveBarrier)
        {
            _barrierManager.CalculateMapBarriers();
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Barrier removed successfully!");
            Logger.LogInfo("Commands", $"{player.PlayerName} removed barrier at bombsite {_editingBombsite}");
        }
        else
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Error removing barrier");
        }
    }

    public void OnCommandTestBarrier(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (_editingBombsite == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must be in barrier editing mode.");
            return;
        }

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Testing barriers for bombsite {_editingBombsite}...");
        _barrierManager.SpawnBarrier((Bombsite)_editingBombsite);

        Logger.LogInfo("Commands", $"{player!.PlayerName} tested barriers for bombsite {_editingBombsite}");
    }

    public void OnCommandDoneBarriers(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        _editingBombsite = null;
        _firstPoint = null;
        _isEditingMode = false;

        Server.ExecuteCommand("mp_warmup_pausetimer 0");
        Server.ExecuteCommand("mp_warmup_end");

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Exited barrier editing mode.");
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Restarting map...");

        _plugin.AddTimer(1.0f, () =>
        {
            Server.ExecuteCommand($"map {Server.MapName}");
        });

        Logger.LogInfo("Commands", $"{player!.PlayerName} exited barrier editing mode");
    }
}