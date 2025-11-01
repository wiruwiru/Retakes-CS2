using System.Text.Json;

using RetakesPlugin.Utils;
using MapBarrier = RetakesPlugin.Models.Barrier;
using MapBarrierConfigData = RetakesPlugin.Models.BarrierConfigData;

namespace RetakesPlugin.Services;

public class BarrierConfigService
{
    private readonly string _mapName;
    private readonly string _barrierConfigDirectory;
    private readonly string _barrierConfigPath;
    private MapBarrierConfigData? _barrierConfigData;
    private readonly JsonSerializerOptions _jsonOptions;

    public BarrierConfigService(string moduleDirectory, string mapName, JsonSerializerOptions jsonOptions)
    {
        _mapName = mapName;
        _barrierConfigDirectory = Path.Combine(moduleDirectory, "map_config", "barriers");
        _barrierConfigPath = Path.Combine(_barrierConfigDirectory, $"{mapName}.json");
        _barrierConfigData = null;
        _jsonOptions = jsonOptions;
    }

    public void Load(bool isViaCommand = false)
    {
        Logger.LogDebug("BarrierConfig", $"Attempting to load barrier data from {_barrierConfigPath}");

        try
        {
            if (!File.Exists(_barrierConfigPath))
            {
                throw new FileNotFoundException();
            }

            var jsonData = File.ReadAllText(_barrierConfigPath);
            _barrierConfigData = JsonSerializer.Deserialize<MapBarrierConfigData>(jsonData, _jsonOptions);

            Logger.LogInfo("BarrierConfig", $"Barrier config loaded for {_mapName}");
        }
        catch (FileNotFoundException)
        {
            Logger.LogWarning("BarrierConfig", $"No barrier config found for map {_mapName}");

            if (!isViaCommand)
            {
                _barrierConfigData = new MapBarrierConfigData();
                Save();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException("BarrierConfig", ex);
        }
    }

    public List<MapBarrier> GetBarriersClone()
    {
        if (_barrierConfigData == null)
        {
            throw new Exception("Barrier config data is null");
        }

        return _barrierConfigData.Barriers.ToList();
    }

    public bool AddBarrier(MapBarrier barrier)
    {
        _barrierConfigData ??= new MapBarrierConfigData();

        if (_barrierConfigData.Barriers.Any(existingBarrier =>
                existingBarrier.MinPos == barrier.MinPos &&
                existingBarrier.MaxPos == barrier.MaxPos &&
                existingBarrier.Bombsite == barrier.Bombsite))
        {
            Logger.LogWarning("BarrierConfig", "Barrier already exists, avoiding duplication");
            return false;
        }

        _barrierConfigData.Barriers.Add(barrier);
        Save();
        Load();

        Logger.LogInfo("BarrierConfig", "Barrier added successfully");
        return true;
    }

    public bool RemoveBarrier(MapBarrier barrier)
    {
        _barrierConfigData ??= new MapBarrierConfigData();

        var barrierToRemove = _barrierConfigData.Barriers.FirstOrDefault(b =>
            b.MinPos == barrier.MinPos &&
            b.MaxPos == barrier.MaxPos &&
            b.Bombsite == barrier.Bombsite);

        if (barrierToRemove == null)
        {
            Logger.LogWarning("BarrierConfig", "Barrier doesn't exist, nothing to remove");
            return false;
        }

        _barrierConfigData.Barriers.Remove(barrierToRemove);
        Save();
        Load();

        Logger.LogInfo("BarrierConfig", "Barrier removed successfully");
        return true;
    }

    private MapBarrierConfigData GetSanitizedBarrierConfigData()
    {
        if (_barrierConfigData == null)
        {
            throw new Exception("Barrier config data is null");
        }

        _barrierConfigData.Barriers = _barrierConfigData.Barriers
            .GroupBy(barrier => new { barrier.MinPos, barrier.MaxPos, barrier.Bombsite })
            .Select(group => group.First())
            .ToList();

        return _barrierConfigData;
    }

    private void Save()
    {
        var jsonString = JsonSerializer.Serialize(GetSanitizedBarrierConfigData(), _jsonOptions);

        try
        {
            if (!Directory.Exists(_barrierConfigDirectory))
            {
                Directory.CreateDirectory(_barrierConfigDirectory);
            }

            File.WriteAllText(_barrierConfigPath, jsonString);
            Logger.LogDebug("BarrierConfig", $"Data written to {_barrierConfigPath}");
        }
        catch (IOException e)
        {
            Logger.LogError("BarrierConfig", $"Error writing to file: {e.Message}");
        }
    }

    public static bool IsLoaded(BarrierConfigService? barrierConfig, string currentMap)
    {
        if (barrierConfig == null || barrierConfig._mapName != currentMap)
        {
            return false;
        }

        return true;
    }
}