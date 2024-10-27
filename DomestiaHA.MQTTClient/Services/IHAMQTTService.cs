using MQTTnet.Client;

namespace DomestiaHA.MQTTClient.Services;
public interface IHAMQTTService
{
    public Task Initialize( IMqttClient client );
    public Task PublishAllLightsStateUpdates();
}
