using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class BarrierSettings
{
    [JsonPropertyName("IsBarrierEnabled")]
    public bool IsBarrierEnabled { get; set; } = false;

    [JsonPropertyName("BarrierRemoveDelay")]
    public float BarrierRemoveDelay { get; set; } = 3.0f;
}