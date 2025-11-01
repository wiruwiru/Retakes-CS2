using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Capabilities;

using RetakesPlugin.Managers;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;
using RetakesPlugin.Commands;
using RetakesPluginShared;
using RetakesPluginShared.Enums;
using RetakesPluginShared.Events;

namespace RetakesPlugin.Events;

public class RoundEventHandlers
{
    private readonly GameManager _gameManager;
    private readonly SpawnManager _spawnManager;
    private readonly BreakerManager? _breakerManager;
    private readonly BarrierManager? _barrierManager;
    private readonly AllocationService _allocationService;
    private readonly AnnouncementService _announcementService;
    private readonly bool _isAutoPlantEnabled;
    private readonly bool _enableFallbackAllocation;
    private readonly bool _enableFallbackBombsiteAnnouncement;
    private readonly bool _isBarrierEnabled;
    private readonly float _barrierRemoveDelay;
    private readonly Random _random;
    private readonly MapConfigService _mapConfigService;
    private SpawnEditorCommands? _spawnEditorCommands;
    private BarrierEditorCommands? _barrierEditorCommands;

    private Bombsite _currentBombsite = Bombsite.A;
    private CCSPlayerController? _planter;
    private CsTeam _lastRoundWinner = CsTeam.None;
    private Bombsite? _forcedBombsite;

    public RoundEventHandlers(GameManager gameManager, SpawnManager spawnManager, BreakerManager? breakerManager, BarrierManager? barrierManager, AllocationService allocationService, AnnouncementService announcementService, bool isAutoPlantEnabled, bool enableFallbackAllocation, bool enableFallbackBombsiteAnnouncement, bool isBarrierEnabled, float barrierRemoveDelay, Random random, MapConfigService mapConfigService)
    {
        _gameManager = gameManager;
        _spawnManager = spawnManager;
        _breakerManager = breakerManager;
        _barrierManager = barrierManager;
        _allocationService = allocationService;
        _announcementService = announcementService;
        _isAutoPlantEnabled = isAutoPlantEnabled;
        _enableFallbackAllocation = enableFallbackAllocation;
        _enableFallbackBombsiteAnnouncement = enableFallbackBombsiteAnnouncement;
        _isBarrierEnabled = isBarrierEnabled;
        _barrierRemoveDelay = barrierRemoveDelay;
        _random = random;
        _mapConfigService = mapConfigService;
    }

    public void SetCommandReferences(SpawnEditorCommands? spawnEditorCommands, BarrierEditorCommands? barrierEditorCommands)
    {
        _spawnEditorCommands = spawnEditorCommands;
        _barrierEditorCommands = barrierEditorCommands;
    }

    public void SetForcedBombsite(Bombsite? bombsite)
    {
        _forcedBombsite = bombsite;
    }

    public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (GameRulesHelper.GetGameRules().WarmupPeriod)
        {
            Logger.LogDebug("Round", "Warmup round, skipping pre-start logic");
            return HookResult.Continue;
        }

        _gameManager.QueueManager.ClearRoundTeams();

        Logger.LogDebug("Round", "Updating queues");
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.Update();
        _gameManager.QueueManager.DebugQueues(false);

        _gameManager.OnRoundPreStart(_lastRoundWinner);
        _gameManager.QueueManager.SetRoundTeams();

        Logger.LogInfo("Round", "Round pre-start complete");
        return HookResult.Continue;
    }

    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Handle weird alive spectators bug
        var weirdAliveSpectators = Utilities.GetPlayers().Where(x => x is { TeamNum: < (int)CsTeam.Terrorist, PawnIsAlive: true });
        foreach (var weirdAliveSpectator in weirdAliveSpectators)
        {
            Server.ExecuteCommand("mp_autoteambalance 0");
            weirdAliveSpectator.CommitSuicide(false, true);
        }

        if (GameRulesHelper.GetGameRules().WarmupPeriod)
        {
            Logger.LogDebug("Round", "Warmup round, skipping.");
            if (_spawnEditorCommands?.ShowingSpawnsForBombsite != null)
            {
                SpawnService.ShowSpawns(null!, _mapConfigService.GetSpawnsClone(),
                    _spawnEditorCommands.ShowingSpawnsForBombsite);
                Logger.LogDebug("Round", $"Re-showing spawns for bombsite {_spawnEditorCommands.ShowingSpawnsForBombsite}");
            }

            if (_barrierEditorCommands?.ShowingBarriersForBombsite != null)
            {
                _barrierManager?.SpawnBarrier((Bombsite)_barrierEditorCommands.ShowingBarriersForBombsite);
                Logger.LogDebug("Round", $"Re-showing barriers for bombsite {_barrierEditorCommands.ShowingBarriersForBombsite}");
            }

            return HookResult.Continue;
        }

        if (_barrierEditorCommands?.IsEditingMode ?? false)
        {
            Logger.LogDebug("Round", "Barrier editing mode active, skipping round logic");
            return HookResult.Continue;
        }

        if (_gameManager == null)
        {
            Logger.LogDebug("Round", "Game manager not loaded.");
            return HookResult.Continue;
        }

        if (_spawnManager == null)
        {
            Logger.LogDebug("Round", "Spawn manager not loaded.");
            return HookResult.Continue;
        }

        _breakerManager?.Handle();
        _currentBombsite = _forcedBombsite ?? (_random.Next(0, 2) == 0 ? Bombsite.A : Bombsite.B);
        _gameManager.ResetPlayerScores();

        if (_isBarrierEnabled && _barrierManager != null)
        {
            _barrierManager.SpawnBarrier(_currentBombsite);
            Logger.LogInfo("Round", $"Barriers spawned for bombsite {_currentBombsite}");
        }

        _planter = _spawnManager.HandleRoundSpawns(_currentBombsite, _gameManager.QueueManager.ActivePlayers);

        if (_enableFallbackBombsiteAnnouncement)
        {
            _announcementService.AnnounceBombsite(_currentBombsite);
        }

        RetakesPlugin.RetakesPluginEventSenderCapability.Get()?.TriggerEvent(new AnnounceBombsiteEvent(_currentBombsite));

        Logger.LogInfo("Round", $"Round started on bombsite {_currentBombsite}");
        return HookResult.Continue;
    }

    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        if (GameRulesHelper.GetGameRules().WarmupPeriod)
        {
            Logger.LogDebug("Round", "Warmup round, skipping post-start logic");
            return HookResult.Continue;
        }

        foreach (var player in _gameManager.QueueManager.ActivePlayers.Where(PlayerHelper.IsValid))
        {
            if (!PlayerHelper.IsValid(player))
            {
                continue;
            }

            PlayerHelper.RemoveHelmetAndHeavyArmour(player);
            player.RemoveWeapons();

            if (player == _planter && !_isAutoPlantEnabled)
            {
                PlayerHelper.GiveAndSwitchToBomb(player);
            }

            if (_enableFallbackAllocation)
            {
                _allocationService.AllocatePlayer(player);
            }
        }

        RetakesPlugin.RetakesPluginEventSenderCapability.Get()?.TriggerEvent(new AllocateEvent());

        Logger.LogInfo("Round", "Round post-start complete");
        return HookResult.Continue;
    }

    public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        if (GameRulesHelper.GetGameRules().WarmupPeriod)
        {
            Logger.LogDebug("Round", "Warmup round, skipping freeze end logic");
            return HookResult.Continue;
        }

        if (_isBarrierEnabled && _barrierManager != null)
        {
            Server.NextFrame(() =>
            {
                var timer = new CounterStrikeSharp.API.Modules.Timers.Timer(_barrierRemoveDelay, () =>
                {
                    _barrierManager.RemoveBarrier();
                    Logger.LogInfo("Round", "Barriers removed after freeze time");
                });
            });
        }

        if (PlayerHelper.GetPlayerCount(CsTeam.Terrorist) > 0)
        {
            HandleAutoPlant();
        }

        return HookResult.Continue;
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        _lastRoundWinner = (CsTeam)@event.Winner;

        if (_isBarrierEnabled && _barrierManager != null)
        {
            _barrierManager.RemoveBarrier();
            Logger.LogDebug("Round", "Barriers cleaned up at round end");
        }

        Logger.LogInfo("Round", $"Round ended. Winner: {_lastRoundWinner}");
        return HookResult.Continue;
    }

    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        Logger.LogInfo("Round", "Bomb planted");

        Task.Run(async () =>
        {
            await Task.Delay(4100);
            _announcementService.AnnounceBombsite(_currentBombsite, true);
        });

        return HookResult.Continue;
    }

    public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (PlayerHelper.IsValid(player))
        {
            _gameManager.AddDefuse(player);
        }

        Logger.LogInfo("Round", $"Bomb defused by {player?.PlayerName ?? "unknown"}");
        return HookResult.Continue;
    }

    private void HandleAutoPlant()
    {
        if (!_isAutoPlantEnabled)
        {
            return;
        }

        if (_planter != null && PlayerHelper.IsValid(_planter))
        {
            BombService.PlantTickingBomb(_planter, _currentBombsite);
            Logger.LogInfo("Round", $"Auto-planted bomb at {_currentBombsite}");
        }
        else
        {
            Logger.LogWarning("Round", "No valid planter found, terminating round");
            GameRulesHelper.TerminateRound(CounterStrikeSharp.API.Modules.Entities.Constants.RoundEndReason.RoundDraw);
        }
    }
}