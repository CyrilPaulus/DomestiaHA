using DomestiaHA.DomestiaProtocol;
using DomestiaHA.MQTTClient;

using Microsoft.Extensions.Logging.Abstractions;

namespace DomestiaHA.Tests;

public class DomestiaHAServiceTests : IDisposable
{
    private static readonly DomestiaLight Relay = new( 1, "Salon", RelayType.Relay );
    private static readonly DomestiaLight Dimmer = new( 2, "Cuisine", RelayType.DimmerContinue );

    private readonly FakePlc _plc = new();
    private readonly DomestiaClient _client;
    private readonly DomestiaHAService _service;

    // Current output values reported by the fake PLC, index 0 is output 1
    private byte[] _values = [0, 0];

    public DomestiaHAServiceTests()
    {
        _plc.Respond = frame => frame[4] == 60 ? [0xFF, 0x00, 2, .. _values] : "OK"u8.ToArray();
        _client = _plc.CreateClient();
        _service = new DomestiaHAService( _client, new MqttBrokerSettings( "localhost", 1883 ), NullLogger<DomestiaHAService>.Instance );
    }

    public void Dispose()
    {
        _service.Dispose();
        _client.Dispose();
        _plc.Dispose();
    }

    [Fact]
    public async Task Off_switches_relay_off()
    {
        await _service.Apply( Relay, new HALightState { State = HALightStateEnum.OFF } );

        Assert.Equal( [FakePlc.Frame( 15, 1 )], _plc.Received );
    }

    [Fact]
    public async Task Off_switches_dimmer_off()
    {
        await _service.Apply( Dimmer, new HALightState { State = HALightStateEnum.OFF, Brightness = 100 } );

        Assert.Equal( [FakePlc.Frame( 15, 2 )], _plc.Received );
    }

    [Fact]
    public async Task On_toggles_relay_when_off()
    {
        _values = [0, 0];

        await _service.Apply( Relay, new HALightState { State = HALightStateEnum.ON } );

        Assert.Equal( [FakePlc.Frame( 60 ), FakePlc.Frame( 13, 1 )], _plc.Received );
    }

    [Fact]
    public async Task On_does_not_toggle_relay_when_already_on()
    {
        _values = [1, 0];

        await _service.Apply( Relay, new HALightState { State = HALightStateEnum.ON } );

        Assert.Equal( [FakePlc.Frame( 60 )], _plc.Received );
    }

    [Fact]
    public async Task On_with_brightness_sets_dimmer_value()
    {
        await _service.Apply( Dimmer, new HALightState { State = HALightStateEnum.ON, Brightness = 255 } );

        Assert.Equal( [FakePlc.Frame( 16, 2, 63 )], _plc.Received );
    }

    [Fact]
    public async Task On_without_brightness_switches_dimmer_on_at_previous_level()
    {
        await _service.Apply( Dimmer, new HALightState { State = HALightStateEnum.ON } );

        Assert.Equal( [FakePlc.Frame( 14, 2 )], _plc.Received );
    }

    [Theory]
    [InlineData( 0, false, 0 )]
    [InlineData( 1, false, 255 )]
    [InlineData( 0, true, 0 )]
    [InlineData( 1, true, 4 )]
    [InlineData( 32, true, 130 )]
    [InlineData( 63, true, 255 )]
    [InlineData( 64, true, 255 )]
    [InlineData( 255, true, 255 )]
    public void ToBrightness( byte value, bool dimmable, int expected )
    {
        Assert.Equal( expected, DomestiaHAService.ToBrightness( value, dimmable ) );
    }

    [Theory]
    [InlineData( -5, 0 )]
    [InlineData( 0, 0 )]
    [InlineData( 1, 1 )]
    [InlineData( 2, 1 )]
    [InlineData( 128, 32 )]
    [InlineData( 255, 63 )]
    [InlineData( 1000, 63 )]
    public void ToDimValue( int brightness, byte expected )
    {
        Assert.Equal( expected, DomestiaHAService.ToDimValue( brightness ) );
    }

    [Fact]
    public void Dim_values_survive_a_round_trip_through_brightness()
    {
        for( byte value = 0; value <= DomestiaClient.MaxDimValue; value++ )
            Assert.Equal( value, DomestiaHAService.ToDimValue( DomestiaHAService.ToBrightness( value, dimmable: true ) ) );
    }
}
