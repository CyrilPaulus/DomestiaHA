using System.Text;

using DomestiaHA.DomestiaProtocol;

namespace DomestiaHA.Tests;

public class DomestiaClientTests : IDisposable
{
    private readonly FakePlc _plc = new();
    private readonly DomestiaClient _client;

    public DomestiaClientTests()
    {
        _client = _plc.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _plc.Dispose();
    }

    [Fact]
    public async Task Commands_are_framed_with_length_and_checksum()
    {
        await _client.Toggle( 1 );
        await _client.SetOn( 2 );
        await _client.SetOff( 3 );
        await _client.SetDimValue( 4, 32 );

        Assert.Equal(
            [
                FakePlc.Frame( 13, 1 ),
                FakePlc.Frame( 14, 2 ),
                FakePlc.Frame( 15, 3 ),
                FakePlc.Frame( 16, 4, 32 ),
            ],
            _plc.Received );
    }

    [Fact]
    public async Task Checksum_wraps_around()
    {
        await _client.SetDimValue( 200, 63 );

        // 16 + 200 + 63 = 279 = 0x117
        Assert.Equal( [0xFF, 0x00, 0x00, 3, 16, 200, 63, 0x17], Assert.Single( _plc.Received ) );
    }

    [Fact]
    public async Task GetOutputTypes_parses_types_indexed_from_1()
    {
        _plc.Respond = _ => [0xFF, 0x00, 0x00, 4, 1, 7, 255, 6];

        var types = await _client.GetOutputTypes();

        Assert.Equal( FakePlc.Frame( 66 ), Assert.Single( _plc.Received ) );
        Assert.Equal(
            new Dictionary<byte, RelayType>
            {
                [1] = RelayType.Relay,
                [2] = RelayType.DimmerContinue,
                [3] = RelayType.Unused,
                [4] = RelayType.DimmerStop,
            },
            types );
    }

    [Fact]
    public async Task GetOutputTypes_ignores_count_larger_than_response()
    {
        _plc.Respond = _ => [0xFF, 0x00, 0x00, 10, 1, 7];

        var types = await _client.GetOutputTypes();

        Assert.Equal( [(byte) 1, (byte) 2], types.Keys );
    }

    [Fact]
    public async Task GetOutputName_stops_at_terminator()
    {
        _plc.Respond = _ => [0xFF, 0x00, 1, 10, .. Encoding.ASCII.GetBytes( "Salon" ), 0xFF, 0xFF];

        var name = await _client.GetOutputName( 5 );

        Assert.Equal( FakePlc.Frame( 62, 5 ), Assert.Single( _plc.Received ) );
        Assert.Equal( "Salon", name );
    }

    [Fact]
    public async Task GetOutputName_handles_empty_name()
    {
        _plc.Respond = _ => [0xFF, 0x00, 1, 10, 0xFF, 0x41, 0x42];

        Assert.Equal( "", await _client.GetOutputName( 1 ) );
    }

    [Fact]
    public async Task GetOutputName_without_terminator_reads_until_end()
    {
        _plc.Respond = _ => [0xFF, 0x00, 1, 10, .. Encoding.ASCII.GetBytes( "Bureau" )];

        Assert.Equal( "Bureau", await _client.GetOutputName( 1 ) );
    }

    [Fact]
    public async Task GetOutputValues_parses_values_indexed_from_1()
    {
        _plc.Respond = _ => [0xFF, 0x00, 3, 0, 1, 63];

        var values = await _client.GetOutputValues();

        Assert.Equal( FakePlc.Frame( 60 ), Assert.Single( _plc.Received ) );
        Assert.Equal( new Dictionary<byte, byte> { [1] = 0, [2] = 1, [3] = 63 }, values );
    }

    [Fact]
    public async Task Too_short_response_throws()
    {
        _plc.Respond = _ => [0xFF, 0x00];

        await Assert.ThrowsAsync<InvalidDataException>( _client.GetOutputTypes );
    }

    [Fact]
    public async Task Connection_is_reused_between_commands()
    {
        await _client.SetOn( 1 );
        await _client.SetOn( 2 );
        await _client.SetOn( 3 );

        Assert.Equal( 1, _plc.ConnectionCount );
    }

    [Fact]
    public async Task Missing_response_times_out_and_next_command_reconnects()
    {
        using var client = _plc.CreateClient( TimeSpan.FromMilliseconds( 200 ) );
        _plc.Respond = _ => null;

        await Assert.ThrowsAsync<TimeoutException>( () => client.SetOn( 1 ) );

        _plc.Respond = _ => "OK"u8.ToArray();
        await client.SetOn( 1 );

        Assert.Equal( 2, _plc.ConnectionCount );
    }

    [Fact]
    public async Task Late_response_is_not_read_as_the_next_answer()
    {
        using var client = _plc.CreateClient( TimeSpan.FromMilliseconds( 200 ) );
        _plc.Respond = _ => "OK"u8.ToArray();
        _plc.ResponseDelay = TimeSpan.FromMilliseconds( 400 );

        await Assert.ThrowsAsync<TimeoutException>( () => client.SetOn( 1 ) );

        _plc.ResponseDelay = TimeSpan.Zero;
        _plc.Respond = frame => frame[4] == 60 ? [0xFF, 0x00, 2, 1, 42] : "OK"u8.ToArray();
        await Task.Delay( 300, TestContext.Current.CancellationToken );

        var values = await client.GetOutputValues();

        Assert.Equal( new Dictionary<byte, byte> { [1] = 1, [2] = 42 }, values );
    }

    [Fact]
    public async Task Closed_connection_throws_and_next_command_reconnects()
    {
        await _client.SetOn( 1 );
        _plc.DropConnections();

        await Assert.ThrowsAnyAsync<IOException>( () => _client.SetOn( 1 ) );
        await _client.SetOn( 1 );

        Assert.Equal( 2, _plc.ConnectionCount );
    }

    [Fact]
    public async Task Unreachable_plc_throws()
    {
        var port = _plc.Port;
        _plc.Dispose();
        using var client = new DomestiaClient( "127.0.0.1", port, TimeSpan.FromMilliseconds( 500 ) );

        await Assert.ThrowsAnyAsync<Exception>( () => client.SetOn( 1 ) );
    }

    [Fact]
    public async Task Concurrent_commands_are_serialized()
    {
        _plc.Respond = frame => [frame[5]];

        await Task.WhenAll( Enumerable.Range( 1, 20 ).Select( i => _client.SetOn( (byte) i ) ) );

        Assert.Equal( 20, _plc.Received.Count );
        Assert.Equal( 1, _plc.ConnectionCount );
    }

    [Theory]
    [InlineData( RelayType.DimmerStop, true )]
    [InlineData( RelayType.DimmerContinue, true )]
    [InlineData( RelayType.Relay, false )]
    [InlineData( RelayType.Toggle, false )]
    [InlineData( RelayType.RGBWhite, false )]
    public void IsDimmable( RelayType type, bool expected )
    {
        Assert.Equal( expected, type.IsDimmable() );
    }
}
