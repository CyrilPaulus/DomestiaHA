using System.Net;
using System.Net.Sockets;

using DomestiaHA.DomestiaProtocol.Commands;
using DomestiaHA.DomestiaProtocol.Responses;

namespace DomestiaHA.DomestiaProtocol;

public class DomestiaConnector : IDisposable
{
    private const int DOMESTIA_TCP_PORT = 52001;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim( 1, 1 );

    public async Task Connect( string ip )
    {
        var ipEndPoint = IPEndPoint.Parse( $"{ip}:{DOMESTIA_TCP_PORT}" );

        _tcpClient = new();
        await _tcpClient.ConnectAsync( ipEndPoint );
        _stream = _tcpClient.GetStream();
    }

    public async Task<TResponse?> ExecuteCommand<TCommand, TResponse>( TCommand command )
        where TCommand : BaseDomestiaCommand
        where TResponse : BaseDomestiaResponse<TCommand>, new()
    {
        await _semaphore.WaitAsync();
        try
        {
            if( _stream == null )
                throw new Exception( "Client should be initialized" );

            var commandBytes = SerializeCommand( command );
            _stream.Write( commandBytes );

            await _stream.FlushAsync();

            // Read response
            var buffer = new byte[1024];
            var count = await _stream.ReadAsync( buffer );
            buffer = buffer.Take( count ).ToArray();

            // Compare CRC
            var crc = ComputeCRC( buffer.Take( buffer.Length - 1 ).ToArray() );
            //if( crc != buffer[^1] )
            //    return null;

            var response = new TResponse();
            response.Deserialize( buffer.Take( buffer.Length - 1 ).ToArray() );
            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }


    private byte[] SerializeCommand( BaseDomestiaCommand command )
    {
        var ms = new MemoryStream();

        // Header

        ms.WriteByte( 0xFF );
        ms.WriteByte( 00 );

        // Data Length
        ms.WriteByte( 0x00 );
        ms.WriteByte( (byte) (command.ParamsCount + 1) );

        // Command id
        ms.WriteByte( command.CommandId );

        // params
        command.Serialize( ms );

        // add CRC
        ms.WriteByte( ComputeCRC( ms.ToArray().Skip( 4 ).ToArray() ) );

        return ms.ToArray();
    }

    private byte ComputeCRC( byte[] bytes )
    {
        var acc = (byte) 0;
        for( int i = 0; i < bytes.Length; i++ )
            acc += bytes[i];

        return acc;
    }

    public void Dispose()
    {
        _tcpClient?.Close();
        _stream?.Dispose();
    }
}