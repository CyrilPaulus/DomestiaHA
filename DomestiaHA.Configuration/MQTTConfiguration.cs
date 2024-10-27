namespace DomestiaHA.Configuration;

public class MQTTConfiguration
{
    public required string BrokerIPAddress { get; set; }
    public int BrokerPort { get; set; }
}