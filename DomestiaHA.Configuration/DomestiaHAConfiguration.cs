using System.Text.Json;

namespace DomestiaHA.Configuration;

public class DomestiaHAConfiguration
{
    public required DomestiaConfiguration Domestia { get; set; }
    public required MQTTConfiguration MQTT { get; set; }
    public required LightConfiguration[] Lights { get; set; }

    public static DomestiaHAConfiguration? ParseConfiguration( string filePath )
    {
        var configStr = File.ReadAllText( filePath );
        return JsonSerializer.Deserialize<DomestiaHAConfiguration>( configStr );
    }
}