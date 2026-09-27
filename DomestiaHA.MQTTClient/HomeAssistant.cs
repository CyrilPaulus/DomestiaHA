using System.Text.Json.Serialization;

namespace DomestiaHA.MQTTClient;

/// <summary>
/// MQTT discovery payload for a JSON schema light.
/// See https://www.home-assistant.io/integrations/light.mqtt/#json-schema
/// </summary>
internal class HALightConfig
{
    [JsonPropertyName( "name" )]
    public required string Name { get; init; }

    [JsonPropertyName( "unique_id" )]
    public required string UniqueId { get; init; }

    [JsonPropertyName( "command_topic" )]
    public required string CommandTopic { get; init; }

    [JsonPropertyName( "state_topic" )]
    public required string StateTopic { get; init; }

    [JsonPropertyName( "availability_topic" )]
    public required string AvailabilityTopic { get; init; }

    [JsonPropertyName( "schema" )]
    public string Schema => "json";

    [JsonPropertyName( "supported_color_modes" )]
    public required string[] SupportedColorModes { get; init; }
}

[JsonConverter( typeof( JsonStringEnumConverter<HALightStateEnum> ) )]
internal enum HALightStateEnum
{
    ON,
    OFF
}

/// <summary>
/// Light state, published on the state topic and received on the command topic.
/// </summary>
internal class HALightState
{
    [JsonPropertyName( "state" )]
    public required HALightStateEnum State { get; init; }

    /// <summary>
    /// Brightness [0, 255]. Absent from commands that only switch the light on or off.
    /// </summary>
    [JsonPropertyName( "brightness" )]
    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    public int? Brightness { get; init; }
}
