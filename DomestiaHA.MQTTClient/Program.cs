
using DomestiaHA.Configuration;
using DomestiaHA.MQTTClient;

using MQTTnet;
using MQTTnet.Client;

var config = DomestiaHAConfiguration.ParseConfiguration( "config.json" ) ?? throw new Exception( "Invalid configuration" );

var domestiaLightService = new FakeDomestiaLightService();
var haMqttService = new HAMQTTService( domestiaLightService, config.Lights );
var mqttFactory = new MqttFactory();

using( var mqttClient = mqttFactory.CreateMqttClient() )
{
    var mqttClientOptions = new MqttClientOptionsBuilder()
        .WithTcpServer( config.MQTT.BrokerIPAddress, config.MQTT.BrokerPort )
        .Build();

    await mqttClient.ConnectAsync( mqttClientOptions, CancellationToken.None );

    await haMqttService.InitializeClient( mqttClient );

    while( true )
    {
        await haMqttService.PublishStateUpdate( mqttClient );
        await Task.Delay( 1000 );
    }

    await mqttClient.DisconnectAsync();

    Console.WriteLine( "MQTT application message is published." );
}