using System.Text.Json;

using DomestiaHA.DomestiaProtocol;
using DomestiaHA.MQTTClient;

namespace DomestiaHA.Tests;

public class HomeAssistantTests
{
    [Theory]
    [InlineData( "Salon", "salon" )]
    [InlineData( "Salle de bain", "salle_de_bain" )]
    [InlineData( "Hall/Entree+1#", "hall_entree_1_" )]
    public void Light_id_is_lowercase_without_spaces_or_mqtt_wildcards( string name, string expectedId )
    {
        var light = new DomestiaLight( 1, name, RelayType.Relay );

        Assert.Equal( expectedId, light.Id );
        Assert.Equal( $"domestia/light/{expectedId}/set", light.CommandTopic );
        Assert.Equal( $"domestia/light/{expectedId}/state", light.StateTopic );
    }

    [Fact]
    public void Config_serializes_to_discovery_payload()
    {
        var config = new HALightConfig
        {
            Name = "Salon",
            UniqueId = "d_salon",
            CommandTopic = "domestia/light/salon/set",
            StateTopic = "domestia/light/salon/state",
            AvailabilityTopic = "domestia/status",
            SupportedColorModes = ["brightness"],
        };

        Assert.Equal(
            """{"name":"Salon","unique_id":"d_salon","command_topic":"domestia/light/salon/set","state_topic":"domestia/light/salon/state","availability_topic":"domestia/status","schema":"json","supported_color_modes":["brightness"]}""",
            JsonSerializer.Serialize( config ) );
    }

    [Fact]
    public void State_serializes_with_brightness()
    {
        var state = new HALightState { State = HALightStateEnum.ON, Brightness = 128 };

        Assert.Equal( """{"state":"ON","brightness":128}""", JsonSerializer.Serialize( state ) );
    }

    [Fact]
    public void State_omits_missing_brightness()
    {
        var state = new HALightState { State = HALightStateEnum.OFF };

        Assert.Equal( """{"state":"OFF"}""", JsonSerializer.Serialize( state ) );
    }

    [Fact]
    public void Command_without_brightness_deserializes_to_null_brightness()
    {
        var command = JsonSerializer.Deserialize<HALightState>( """{"state":"ON"}""" )!;

        Assert.Equal( HALightStateEnum.ON, command.State );
        Assert.Null( command.Brightness );
    }

    [Fact]
    public void Command_with_brightness_deserializes()
    {
        var command = JsonSerializer.Deserialize<HALightState>( """{"state":"ON","brightness":42}""" )!;

        Assert.Equal( HALightStateEnum.ON, command.State );
        Assert.Equal( 42, command.Brightness );
    }

    [Fact]
    public void Command_with_unknown_state_throws()
    {
        Assert.Throws<JsonException>( () => JsonSerializer.Deserialize<HALightState>( """{"state":"DIM"}""" ) );
    }
}
