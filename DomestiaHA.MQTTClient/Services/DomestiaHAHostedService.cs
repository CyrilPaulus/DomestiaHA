using DomestiaHA.Configuration;

using Microsoft.Extensions.Hosting;

using MQTTnet;
using MQTTnet.Client;

namespace DomestiaHA.MQTTClient.Services;
internal class DomestiaHAHostedService : BackgroundService
{
    private readonly IDomestiaHAConfigurationService configurationService;
    private readonly IHAMQTTService _haMQTTService;

    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds( 10 );

    public DomestiaHAHostedService(
        IDomestiaHAConfigurationService domestiaHAConfigurationService,
        IHAMQTTService haMQTTService )
    {
        configurationService = domestiaHAConfigurationService;
        _haMQTTService = haMQTTService;
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        var mqttConfiguration = configurationService.GetMQTTConfiguration();

        var mqttClientOptions = new MqttClientOptionsBuilder()
        .WithTcpServer( mqttConfiguration.BrokerIPAddress, mqttConfiguration.BrokerPort )
        .Build();

        await mqttClient.ConnectAsync( mqttClientOptions, stoppingToken );

        await _haMQTTService.Initialize( mqttClient );

        while( !stoppingToken.IsCancellationRequested )
        {
            await _haMQTTService.PublishAllLightsStateUpdates();
            await Task.Delay( _refreshInterval, stoppingToken );
        }

        await mqttClient.DisconnectAsync();
    }
}
