using System.Text.Json;

using DomestiaHA.DomestiaProtocol;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MQTTnet;

namespace DomestiaHA.MQTTClient;

internal record MqttBrokerSettings( string Host, int Port );

internal record DomestiaLight( byte Output, string Name, RelayType Type )
{
    // Kept identical to previous versions so Home Assistant unique ids don't change; MQTT wildcards are stripped
    public string Id { get; } = Name.ToLowerInvariant().Replace( ' ', '_' ).Replace( '/', '_' ).Replace( '+', '_' ).Replace( '#', '_' );

    public bool Dimmable => Type.IsDimmable();

    public string CommandTopic => $"domestia/light/{Id}/set";

    public string StateTopic => $"domestia/light/{Id}/state";
}

/// <summary>
/// Bridges the Domestia lights to Home Assistant through MQTT discovery.
/// </summary>
internal class DomestiaHAService(
    DomestiaClient domestia,
    MqttBrokerSettings broker,
    ILogger<DomestiaHAService> logger ) : BackgroundService
{
    private const string AvailabilityTopic = "domestia/status";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds( 1 );
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds( 5 );

    // Released after a command to refresh the state immediately instead of waiting for the next poll
    private readonly SemaphoreSlim _refresh = new( 0 );

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        while( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                await Run( stoppingToken );
            }
            catch( OperationCanceledException ) when( stoppingToken.IsCancellationRequested )
            {
            }
            catch( Exception e )
            {
                logger.LogError( e, "Bridge failed, restarting in {delay}", RetryDelay );
                await Task.Delay( RetryDelay, stoppingToken ).ConfigureAwait( ConfigureAwaitOptions.SuppressThrowing );
            }
        }
    }

    private async Task Run( CancellationToken stoppingToken )
    {
        var lights = await LoadLights();

        using var mqtt = new MqttClientFactory().CreateMqttClient();
        mqtt.ApplicationMessageReceivedAsync += e => OnCommand( e, lights );

        logger.LogInformation( "Connecting to MQTT broker {host}:{port}", broker.Host, broker.Port );
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer( broker.Host, broker.Port )
            .WithWillTopic( AvailabilityTopic )
            .WithWillPayload( "offline" )
            .WithWillRetain()
            .Build();
        await mqtt.ConnectAsync( options, stoppingToken );

        try
        {
            foreach( var light in lights )
            {
                var config = new HALightConfig
                {
                    Name = light.Name.Trim(),
                    UniqueId = $"d_{light.Id}",
                    CommandTopic = light.CommandTopic,
                    StateTopic = light.StateTopic,
                    AvailabilityTopic = AvailabilityTopic,
                    SupportedColorModes = [light.Dimmable ? "brightness" : "onoff"],
                };
                await Publish( mqtt, $"homeassistant/light/{light.Id}/config", JsonSerializer.Serialize( config ), stoppingToken );
                await mqtt.SubscribeAsync( light.CommandTopic, cancellationToken: stoppingToken );
            }

            await Publish( mqtt, AvailabilityTopic, "online", stoppingToken );

            // Last brightness published per output, to only publish changes
            var published = new Dictionary<byte, int>();
            while( true )
            {
                await PublishStates( mqtt, lights, published, stoppingToken );
                await _refresh.WaitAsync( PollInterval, stoppingToken );
            }
        }
        finally
        {
            if( mqtt.IsConnected )
            {
                // The will message is only sent on unexpected disconnections
                await Publish( mqtt, AvailabilityTopic, "offline", CancellationToken.None );
                await mqtt.DisconnectAsync();
            }
        }
    }

    private async Task<List<DomestiaLight>> LoadLights()
    {
        logger.LogInformation( "Loading Domestia configuration" );

        var lights = new Dictionary<string, DomestiaLight>();
        foreach( var (output, type) in await domestia.GetOutputTypes() )
        {
            if( type == RelayType.Unused )
                continue;

            var light = new DomestiaLight( output, await domestia.GetOutputName( output ), type );
            if( !lights.TryAdd( light.Id, light ) )
            {
                logger.LogWarning( "Ignoring output {output}: duplicate name '{name}'", output, light.Name );
                continue;
            }

            logger.LogInformation( "Output {output}: '{name}' ({type})", output, light.Name, type );
        }

        return lights.Values.ToList();
    }

    private async Task PublishStates( IMqttClient mqtt, List<DomestiaLight> lights, Dictionary<byte, int> published, CancellationToken cancellationToken )
    {
        var values = await domestia.GetOutputValues();

        foreach( var light in lights )
        {
            if( !values.TryGetValue( light.Output, out var value ) )
            {
                logger.LogWarning( "No value received for '{name}'", light.Name );
                continue;
            }

            var brightness = ToBrightness( value, light.Dimmable );
            if( published.TryGetValue( light.Output, out var previous ) && previous == brightness )
                continue;

            var state = new HALightState
            {
                State = brightness > 0 ? HALightStateEnum.ON : HALightStateEnum.OFF,
                Brightness = brightness,
            };
            await Publish( mqtt, light.StateTopic, JsonSerializer.Serialize( state ), cancellationToken );
            published[light.Output] = brightness;
        }
    }

    private async Task OnCommand( MqttApplicationMessageReceivedEventArgs e, List<DomestiaLight> lights )
    {
        var light = lights.FirstOrDefault( x => x.CommandTopic == e.ApplicationMessage.Topic );
        if( light is null )
            return;

        try
        {
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            logger.LogInformation( "Command for '{name}': {payload}", light.Name, payload );

            var command = JsonSerializer.Deserialize<HALightState>( payload )
                ?? throw new InvalidDataException( "Empty command" );

            await Apply( light, command );
            _refresh.Release();
        }
        catch( Exception ex )
        {
            logger.LogError( ex, "Failed to handle command for '{name}'", light.Name );
        }
    }

    private async Task Apply( DomestiaLight light, HALightState command )
    {
        if( command.State == HALightStateEnum.OFF )
        {
            await domestia.SetOff( light.Output );
            return;
        }

        if( light.Dimmable )
        {
            // Without brightness, switch on at the previous level
            if( command.Brightness is int brightness )
                await domestia.SetDimValue( light.Output, ToDimValue( brightness ) );
            else
                await domestia.SetOn( light.Output );
            return;
        }

        // Relays are switched on with a toggle, so only toggle when off
        var values = await domestia.GetOutputValues();
        if( values.GetValueOrDefault( light.Output ) == 0 )
            await domestia.Toggle( light.Output );
    }

    private static int ToBrightness( byte value, bool dimmable )
    {
        if( !dimmable )
            return value > 0 ? 255 : 0;

        return (int) Math.Round( Math.Min( value, DomestiaClient.MaxDimValue ) * 255.0 / DomestiaClient.MaxDimValue );
    }

    private static byte ToDimValue( int brightness )
    {
        if( brightness <= 0 )
            return 0;

        // Any non-zero brightness must stay on
        var value = (int) Math.Round( Math.Min( brightness, 255 ) * DomestiaClient.MaxDimValue / 255.0 );
        return (byte) Math.Max( value, 1 );
    }

    private static async Task Publish( IMqttClient mqtt, string topic, string payload, CancellationToken cancellationToken )
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic( topic )
            .WithPayload( payload )
            .WithRetainFlag()
            .Build();

        var result = await mqtt.PublishAsync( message, cancellationToken );
        if( !result.IsSuccess )
            throw new InvalidOperationException( $"Can't publish to {topic}: {result.ReasonString}" );
    }
}
