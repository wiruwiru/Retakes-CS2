using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Managers;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Events;

public class PlayerEventHandlers
{
    private readonly RetakesPlugin _plugin;
    private readonly GameManager _gameManager;
    private readonly HashSet<CCSPlayerController> _hasMutedVoices;

    public PlayerEventHandlers(RetakesPlugin plugin, GameManager gameManager, HashSet<CCSPlayerController> hasMutedVoices)
    {
        _plugin = plugin;
        _gameManager = gameManager;
        _hasMutedVoices = hasMutedVoices;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!PlayerHelper.IsValid(player))
        {
            return HookResult.Continue;
        }

        player.ForceTeamTime = 3600.0f;

        // Add small delay to ensure player is fully connected
        _plugin.AddTimer(1.0f, () =>
        {
            if (!PlayerHelper.IsValid(player) || _gameManager == null)
            {
                return;
            }

            // Check if player is already in queue system
            var isInQueue = _gameManager.QueueManager.QueuePlayers.Contains(player);
            var isInActivePlayers = _gameManager.QueueManager.ActivePlayers.Contains(player);

            if (!isInQueue && !isInActivePlayers)
            {
                var willBeFirstPlayer = _gameManager.QueueManager.ActivePlayers.Count == 0 && _gameManager.QueueManager.QueuePlayers.Count == 0;
                if (willBeFirstPlayer)
                {
                    // First player: assign directly to default team (Terrorist)
                    Logger.LogDebug("Player", $"[{player.PlayerName}] First player - assigning directly to team");

                    _gameManager.QueueManager.ClearRoundTeams();
                    _gameManager.QueueManager.DebugQueues(true);

                    // Add player directly to active players
                    _gameManager.QueueManager.PlayerJoinedTeam(player, CsTeam.None, CsTeam.Terrorist);
                    player.ChangeTeam(CsTeam.Terrorist);

                    _gameManager.QueueManager.Update();
                    _gameManager.QueueManager.DebugQueues(false);

                    Logger.LogDebug("Player", $"[{player.PlayerName}] First player added. ActivePlayers count: {_gameManager.QueueManager.ActivePlayers.Count}");
                    GameRulesHelper.RestartGame();
                }
                else
                {
                    if (_plugin.Config.Game.AutoAddToQueue)
                    {
                        // Not first player: Send to spectator and add to queue
                        player.ChangeTeam(CsTeam.Spectator);

                        Logger.LogDebug("Player", $"[{player.PlayerName}] Adding new player to queue via OnPlayerConnectFull");

                        _gameManager.QueueManager.ClearRoundTeams();

                        // Add player to queue system
                        _gameManager.QueueManager.DebugQueues(true);
                        _gameManager.QueueManager.QueuePlayers.Add(player);
                        _gameManager.QueueManager.DebugQueues(false);

                        Logger.LogDebug("Player", $"[{player.PlayerName}] Player added to queue. ActivePlayers count: {_gameManager.QueueManager.ActivePlayers.Count}");
                    }
                }
            }

        });

        // Grant VIP to contributors
        if (new List<ulong> { 76561198028510846, 76561198044886803, 76561198414501446, 76561199074660131 }.Contains(player.SteamID))
        {
            var grant = "@css/vip";
            Logger.LogInfo("Queue", $"You have been given queue priority {grant} for being a Retakes contributor!");
            AdminManager.AddPlayerPermissions(player, grant);
            Logger.LogInfo("Player", $"Granted VIP to contributor {player.PlayerName}");
        }

        Logger.LogInfo("Player", $"{player.PlayerName} connected");
        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!PlayerHelper.IsValid(player) || !PlayerHelper.IsConnected(player))
        {
            return HookResult.Continue;
        }

        Logger.LogDebug("Player", $"[{player.PlayerName}] Spawned");

        if (!_gameManager.QueueManager.ActivePlayers.Contains(player))
        {
            if (player.PlayerPawn.Value != null && player.PlayerPawn.IsValid && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.CommitSuicide(false, true);
            }

            if (!player.IsBot)
            {
                player.ChangeTeam(CsTeam.Spectator);
            }
            else if (!player.IsHLTV)
            {
                _gameManager.QueueManager.ActivePlayers.Add(player);
                Logger.LogInfo("Player", $"Force added bot {player.PlayerName} to active players");
            }

            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var assister = @event.Assister;

        if (PlayerHelper.IsValid(attacker))
        {
            _gameManager.AddKill(attacker);
        }

        if (PlayerHelper.IsValid(assister))
        {
            _gameManager.AddAssist(assister);
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        _gameManager.QueueManager.RemovePlayerFromQueues(player);
        _hasMutedVoices.Remove(player);

        Logger.LogInfo("Player", $"{player.PlayerName} disconnected");
        return HookResult.Continue;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        @event.Silent = true;
        return _gameManager.RemoveSpectators(@event, _hasMutedVoices);
    }
}