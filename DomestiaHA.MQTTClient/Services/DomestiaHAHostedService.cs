using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<DomestiaHAHostedService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(1);

    public DomestiaHAHostedService(
        ILogger<DomestiaHAHostedService> logger,
        IOptions<DomestiaHAHosetedServiceConfiguration> options,
        IServiceScopeFactory serviceScopeFactory)
    {
        _options = options.Value;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Run(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while running main code");
                await Task.Delay(_refreshInterval, stoppingToken);
            }
        }
    }

    private async Task Run(CancellationToken stoppingToken) 
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var haMQTTService = scope.ServiceProvider.GetRequiredService<IHAMQTTService>();
        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        _logger.LogInformation("Connecting to MQTT Broker {ipAddres}:{port}", _options.BrokerIPAddress, _options.BrokerPort);

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.BrokerIPAddress, _options.BrokerPort)
            .Build();

        await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);

        await haMQTTService.Initialize(mqttClient);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await haMQTTService.PublishAllLightsStateUpdates();
                await Task.Delay(_refreshInterval, stoppingToken);
            }
        }
        finally
        {

            await mqttClient.DisconnectAsync();
        }
    }
}
