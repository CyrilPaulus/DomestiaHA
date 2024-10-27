using System.Text.Json.Serialization;

namespace DomestiaHA.MQTTClient.HAEntities;


public enum HALightStateEnum
{
    ON,
    OFF
}

public class HALightState
{
    [JsonPropertyName( "state" )]
    [JsonConverter( typeof( JsonStringEnumConverter ) )]
    public required HALightStateEnum State { get; set; }


    /// <summary>
    /// Brigthness between 0-255
    /// </summary>
    [JsonPropertyName( "brightness" )]
    public int Brightness { get; set; }
}