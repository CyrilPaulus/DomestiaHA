﻿using System.Text.Json;
using System.Text.RegularExpressions;

using DomestiaHA.Abstraction;
using DomestiaHA.Configuration;
using DomestiaHA.Configuration.Models;
using DomestiaHA.MQTTClient.HAEntities;
using DomestiaHA.MQTTClient.Services;

using MQTTnet;
using MQTTnet.Client;

namespace DomestiaHA.MQTTClient;

internal partial class HAMQTTService(
    IDomestiaLightService domestiaLightService,
    IDomestiaHAConfigurationService configurationService ) : IHAMQTTService
{
    private readonly IDomestiaLightService _domestiaLightService = domestiaLightService;
    private readonly IEnumerable<LightConfiguration> _lights = configurationService.GetLightConfigurations();

    private IMqttClient? _client;

    public async Task Initialize( IMqttClient client )
    {
        _client = client;
        _client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;

        foreach( var light in _lights )
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
        }
    }

    public async Task PublishAllLightsStateUpdates()
    {
        foreach( var light in _lights )
        {
            await PublishLigthStateUpdate( light );
        }
    }

    private async Task PublishLigthStateUpdate( LightConfiguration light )
    {
        var lightId = GetLightId( light );
        var brigthness = _domestiaLightService.GetBrigthness( lightId );
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
        var light = _lights.FirstOrDefault( x => GetLightId( x ) == lightId );
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

        _domestiaLightService.SetBrigthness( lightId, brightness );

        await PublishLigthStateUpdate( light );
    }

    private HALight ConvertLightConfiguration( LightConfiguration lightConfiguration )
    {
        var lightId = GetLightId( lightConfiguration );
        return new HALight()
        {
            Name = lightConfiguration.Label,
            UniqueId = $"d_{lightConfiguration.RelayId}",
            CommandTopic = $"domestia/light/{lightId}/set",
            StateTopic = $"domestia/light/{lightId}/state",
            Brightness = lightConfiguration.Dimmable,
            Schema = "json"
        };
    }

    private string GetLightId( LightConfiguration lightConfiguration )
    {
        return lightConfiguration.Label.ToLowerInvariant().Replace( " ", "_" );
    }

    [GeneratedRegex( "domestia/light/(.*)/set" )]
    private static partial Regex MyRegex();
}