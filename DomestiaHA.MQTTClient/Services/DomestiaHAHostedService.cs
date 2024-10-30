﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MQTTnet;
using MQTTnet.Client;

namespace DomestiaHA.MQTTClient.Services;

internal class DomestiaHAHosetedServiceConfiguration
{
    public required string BrokerIPAddress { get; set; }
    public int BrokerPort { get; set; }
}

internal class DomestiaHAHostedService : BackgroundService
{
    private readonly DomestiaHAHosetedServiceConfiguration _options;
    private readonly IHAMQTTService _haMQTTService;

    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds( 1 );

    public DomestiaHAHostedService(
        IOptions<DomestiaHAHosetedServiceConfiguration> options,
        IHAMQTTService haMQTTService )
    {
        _options = options.Value;
        _haMQTTService = haMQTTService;
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
        .WithTcpServer( _options.BrokerIPAddress, _options.BrokerPort )
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
