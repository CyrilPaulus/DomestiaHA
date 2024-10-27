namespace DomestiaHA.Configuration.Models;

public class DomestiaHAConfiguration
{
    public required DomestiaConfiguration Domestia { get; set; }
    public required MQTTConfiguration MQTT { get; set; }
    public required LightConfiguration[] Lights { get; set; }

}