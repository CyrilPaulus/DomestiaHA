using MQTTnet.Client;

namespace DomestiaHA.MQTTClient.Services;
public interface IHAMQTTService
{
    public Task InitializeClient( IMqttClient client );
    public Task PublishStateUpdate( IMqttClient client );
}
