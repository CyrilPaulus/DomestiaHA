using System.Text.Json;
using System.Text.RegularExpressions;

using DomestiaHA.Abstraction;
using DomestiaHA.Abstraction.Models;

using DomestiaHA.MQTTClient.HAEntities;
using DomestiaHA.MQTTClient.Services;

using MQTTnet;
using MQTTnet.Client;

namespace DomestiaHA.MQTTClient;

internal partial class HAMQTTService( ILightService domestiaLightService ) : IHAMQTTService
{
    private readonly ILightService _domestiaLightService = domestiaLightService;


    private Dictionary<string, Light> _lights = new Dictionary<string, Light>();
    private IMqttClient? _client;

    public async Task Initialize( IMqttClient client )
    {
        await _domestiaLightService.Connect();

        _client = client;
        _client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;

        var lights = _domestiaLightService.GetLights();

        foreach( var light in lights )
        {
            var lightId = GetLightId( light );
            var haLight = ConvertLightConfiguration( light );
            var topic = $"homeassistant/light/{lightId}/config";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic( topic )
                .WithPayload( JsonSerializer.Serialize( haLight ) )
                .Build();

            var publishResult = await client.PublishAsync( message );
            if( !publishResult.IsSuccess )
                throw new InvalidOperationException( $"Can't publish light: {light.Label}" );

            var commandTopicSubscription = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter( haLight.CommandTopic )
                .WithTopicFilter( haLight.StateTopic )
                .Build();

            await client.SubscribeAsync( commandTopicSubscription );

            _lights.Add( lightId, light );
        }
    }

    public async Task PublishAllLightsStateUpdates()
    {
        var allBrightness = await _domestiaLightService.GetAllBrigthness();

        foreach( var light in _lights.Values )
        {
            await PublishLigthStateUpdate( light, allBrightness[light.Label] );
        }
    }

    private async Task PublishLigthStateUpdate(
        Light light,
        int brigthness )
    {
        var haLight = ConvertLightConfiguration( light );
        var haLightState = new HALightState()
        {
            State = brigthness > 0 ? HALightStateEnum.ON : HALightStateEnum.OFF,
            Brightness = brigthness,
        };

        var message = new MqttApplicationMessageBuilder()
            .WithTopic( haLight.StateTopic )
            .WithPayload( JsonSerializer.Serialize( haLightState ) )
            .Build();

        var publishResult = await _client!.PublishAsync( message );
        if( !publishResult.IsSuccess )
            throw new InvalidOperationException( $"Can't publish light: {light.Label}" );
    }

    private async Task Client_ApplicationMessageReceivedAsync( MqttApplicationMessageReceivedEventArgs arg )
    {
        var stateStr = arg.ApplicationMessage.ConvertPayloadToString();
        var regex = MyRegex();
        var match = regex.Match( arg.ApplicationMessage.Topic );
        if( !match.Success )
            return;

        var lightId = match.Groups[1].Value;
        var light = _lights[lightId];
        if( light is null )
            return;

        var haLightState = JsonSerializer.Deserialize<HALightState>( stateStr )!;

        var brightness = (light.Dimmable, haLightState.State) switch
        {
            (true, _ ) => haLightState.Brightness,
            (false, HALightStateEnum.ON ) => 255,
            (false, HALightStateEnum.OFF ) => 0,
            _ => throw new InvalidOperationException()
        };

        await _domestiaLightService.SetBrigthness( light, brightness );

        var brigthness = await _domestiaLightService.GetBrigthness( light );

        await PublishLigthStateUpdate( light, brigthness );
    }

    private HALight ConvertLightConfiguration( Light light )
    {
        var lightId = GetLightId( light );
        return new HALight()
        {
            Name = lightId,
            UniqueId = $"d_{lightId}",
            CommandTopic = $"domestia/light/{lightId}/set",
            StateTopic = $"domestia/light/{lightId}/state",
            Brightness = light.Dimmable,
            Schema = "json"
        };
    }

    private string GetLightId( Light light )
    {
        return light.Label.ToLowerInvariant().Replace( " ", "_" );
    }

    [GeneratedRegex( "domestia/light/(.*)/set" )]
    private static partial Regex MyRegex();
}