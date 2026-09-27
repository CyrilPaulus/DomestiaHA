using System.Net.Sockets;
using System.Text;

namespace DomestiaHA.DomestiaProtocol;

/// <summary>
/// Client for the Domestia PLC TCP protocol.
/// The connection is opened on demand and dropped after any error, so the next call reconnects.
/// </summary>
public sealed class DomestiaClient( string host, int port = DomestiaClient.DefaultPort, TimeSpan? timeout = null ) : IDisposable
{
    public const byte MaxDimValue = 63;
    public const int DefaultPort = 52001;

    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds( 2 );

    private readonly SemaphoreSlim _lock = new( 1, 1 );
    private TcpClient? _tcpClient;

    public async Task<Dictionary<byte, RelayType>> GetOutputTypes()
    {
        var data = await Send( 66 );
        EnsureLength( data, 4 );

        var count = Math.Min( data[3], data.Length - 4 );
        var types = new Dictionary<byte, RelayType>();
        for( var i = 0; i < count; i++ )
            types[(byte) (i + 1)] = (RelayType) data[i + 4];

        return types;
    }

    public async Task<string> GetOutputName( byte output )
    {
        var data = await Send( 62, output );
        EnsureLength( data, 4 );

        // Name is ASCII, terminated by 0xFF
        var name = data.AsSpan( 4 );
        var end = name.IndexOf( (byte) 0xFF );
        return Encoding.ASCII.GetString( end >= 0 ? name[..end] : name );
    }

    /// <summary>
    /// Returns the raw value of every output, indexed from 1.
    /// Relays report 0 (off) or 1 (on), dimmers report [0, <see cref="MaxDimValue"/>].
    /// </summary>
    public async Task<Dictionary<byte, byte>> GetOutputValues()
    {
        var data = await Send( 60 );

        var values = new Dictionary<byte, byte>();
        for( var i = 3; i < data.Length; i++ )
            values[(byte) (i - 2)] = data[i];

        return values;
    }

    public Task Toggle( byte output ) => Send( 13, output );

    public Task SetOn( byte output ) => Send( 14, output );

    public Task SetOff( byte output ) => Send( 15, output );

    public Task SetDimValue( byte output, byte value ) => Send( 16, output, value );

    /// <summary>
    /// Sends a command and returns the response without its trailing checksum byte.
    /// </summary>
    private async Task<byte[]> Send( byte commandId, params byte[] args )
    {
        // Frame: FF 00 00 <payload length> <payload> <checksum>, where payload = command id + args
        byte[] payload = [commandId, .. args];
        byte[] frame = [0xFF, 0x00, 0x00, (byte) payload.Length, .. payload, Checksum( payload )];

        await _lock.WaitAsync();
        try
        {
            using var cts = new CancellationTokenSource( _timeout );

            if( _tcpClient is not { Connected: true } )
            {
                _tcpClient?.Dispose();
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync( host, port, cts.Token );
            }

            var stream = _tcpClient.GetStream();
            await stream.WriteAsync( frame, cts.Token );

            var buffer = new byte[1024];
            var count = await stream.ReadAsync( buffer, cts.Token );
            if( count == 0 )
                throw new IOException( "Connection closed by the Domestia PLC" );

            return buffer[..(count - 1)];
        }
        catch( Exception e )
        {
            // Drop the connection: a late or partial response would otherwise be read as the answer to the next command
            _tcpClient?.Dispose();
            _tcpClient = null;

            if( e is OperationCanceledException )
                throw new TimeoutException( $"Domestia PLC did not answer command {commandId} within {_timeout}", e );
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static byte Checksum( byte[] bytes )
    {
        byte sum = 0;
        foreach( var b in bytes )
            sum += b;
        return sum;
    }

    private static void EnsureLength( byte[] data, int length )
    {
        if( data.Length < length )
            throw new InvalidDataException( $"Domestia response too short: {Convert.ToHexString( data )}" );
    }

    public void Dispose()
    {
        _tcpClient?.Dispose();
        _lock.Dispose();
    }
}
