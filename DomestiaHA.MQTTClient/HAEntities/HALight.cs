using System.Text.Json.Serialization;

namespace DomestiaHA.MQTTClient.HAEntities;

internal class HALight
{
    [JsonPropertyName( "name" )]
    public required string Name { get; set; }

    [JsonPropertyName( "unique_id" )]
    public required string UniqueId { get; set; }

    [JsonPropertyName( "command_topic" )]
    public required string CommandTopic { get; set; }

    [JsonPropertyName( "state_topic" )]
    public required string StateTopic { get; set; }

    [JsonPropertyName( "schema" )]
    public required string Schema { get; set; }

    [JsonPropertyName( "brightness" )]
    public required bool Brightness { get; set; }
}