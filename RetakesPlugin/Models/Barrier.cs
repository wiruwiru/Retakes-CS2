
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs.JsonConverters;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Models;

public class Barrier
{
    public Barrier(Vector minPos, Vector maxPos)
    {
        MinPos = minPos;
        MaxPos = maxPos;
    }

    [JsonConverter(typeof(VectorJsonConverter))]
    public Vector MinPos { get; }

    [JsonConverter(typeof(VectorJsonConverter))]
    public Vector MaxPos { get; }

    public Bombsite Bombsite { get; set; }
}