using System.Numerics;
using System.Runtime.InteropServices;

using RetakesPlugin.Services;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;
using MapBarrier = RetakesPlugin.Models.Barrier;

namespace RetakesPlugin.Managers;

public class BarrierManager
{
    private readonly BarrierConfigService _barrierConfigService;
    private readonly Dictionary<Bombsite, List<MapBarrier>> _barriers = new();
    private readonly Random _random = new();

    public delegate void RetakesBarrierSpawnDelegate(bool a1, Vector3 minPos, Vector3 maxPos, float a4);
    private RetakesBarrierSpawnDelegate? _retakesBarrierSpawn;

    public BarrierManager(BarrierConfigService barrierConfigService)
    {
        _barrierConfigService = barrierConfigService;

        InitializeNativeFunction();

        CalculateMapBarriers();
    }

    private void InitializeNativeFunction()
    {
        string signature = "55 66 48 0F 7E D0";

        try
        {
            IntPtr functionPtr = CounterStrikeSharp.API.Core.NativeAPI.FindSignature("libserver.so", signature);
            if (functionPtr == IntPtr.Zero)
            {
                Logger.LogError("BarrierManager", "Could not find the barrier signature in libserver.so");
                return;
            }

            Logger.LogInfo("BarrierManager", $"Found barrier signature at 0x{functionPtr:X}");
            _retakesBarrierSpawn = Marshal.GetDelegateForFunctionPointer<RetakesBarrierSpawnDelegate>(functionPtr);
            Logger.LogInfo("BarrierManager", "Successfully loaded RetakesBarrierSpawn delegate");
        }
        catch (Exception ex)
        {
            Logger.LogException("BarrierManager", ex);
        }
    }

    public void CalculateMapBarriers()
    {
        _barriers.Clear();

        _barriers.Add(Bombsite.A, []);
        _barriers.Add(Bombsite.B, []);

        try
        {
            foreach (var barrier in _barrierConfigService.GetBarriersClone())
            {
                _barriers[barrier.Bombsite].Add(barrier);
            }

            Logger.LogInfo("BarrierManager", "Map barriers calculated successfully");
        }
        catch (Exception ex)
        {
            Logger.LogWarning("BarrierManager", $"No barriers configured: {ex.Message}");
        }
    }

    public List<MapBarrier> GetBarriers(Bombsite bombsite)
    {
        if (!_barriers.ContainsKey(bombsite))
        {
            Logger.LogWarning("BarrierManager", $"No barriers found for bombsite {bombsite}");
            return [];
        }

        return _barriers[bombsite];
    }

    public void SpawnBarrier(Bombsite bombsite)
    {
        if (_retakesBarrierSpawn == null)
        {
            Logger.LogWarning("BarrierManager", "Barrier spawn function not available");
            return;
        }

        var barriers = GetBarriers(bombsite);

        if (barriers.Count == 0)
        {
            Logger.LogDebug("BarrierManager", $"No barriers configured for bombsite {bombsite}");
            return;
        }

        foreach (var barrier in barriers)
        {
            try
            {
                var minPos = new Vector3(barrier.MinPos.X, barrier.MinPos.Y, barrier.MinPos.Z);
                var maxPos = new Vector3(barrier.MaxPos.X, barrier.MaxPos.Y, barrier.MaxPos.Z);

                _retakesBarrierSpawn(false, minPos, maxPos, 0f);

                Logger.LogDebug("BarrierManager", $"Barrier spawned for bombsite {bombsite}");
            }
            catch (Exception ex)
            {
                Logger.LogException("BarrierManager", ex);
            }
        }
    }

    public void RemoveBarrier()
    {
        if (_retakesBarrierSpawn == null)
        {
            return;
        }

        try
        {
            _retakesBarrierSpawn(true, new Vector3(0, 0, 0), new Vector3(0, 0, 0), 0f);
            Logger.LogDebug("BarrierManager", "Barriers removed");
        }
        catch (Exception ex)
        {
            Logger.LogException("BarrierManager", ex);
        }
    }
}