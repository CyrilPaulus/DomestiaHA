namespace DomestiaHA.Configuration.Models;

public class MQTTConfiguration
{
    public required string BrokerIPAddress { get; set; }
    public int BrokerPort { get; set; }
}