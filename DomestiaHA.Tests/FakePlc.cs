using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using DomestiaHA.DomestiaProtocol;

namespace DomestiaHA.Tests;

/// <summary>
/// Fake Domestia PLC listening on a random loopback port.
/// </summary>
internal sealed class FakePlc : IDisposable
{
    private readonly TcpListener _listener = new( IPAddress.Loopback, 0 );
    private readonly CancellationTokenSource _cts = new();
    private readonly List<TcpClient> _connections = [];

    public FakePlc()
    {
        _listener.Start();
        _ = AcceptLoop();
    }

    public int Port => ((IPEndPoint) _listener.LocalEndpoint).Port;

    /// <summary>
    /// Builds the response to a received frame, without checksum. Returns null to not answer.
    /// </summary>
    public Func<byte[], byte[]?> Respond { get; set; } = _ => "OK"u8.ToArray();

    /// <summary>
    /// Delay before answering, to simulate a slow PLC.
    /// </summary>
    public TimeSpan ResponseDelay { get; set; }

    public ConcurrentQueue<byte[]> Received { get; } = new();

    public int ConnectionCount { get { lock( _connections ) return _connections.Count; } }

    public DomestiaClient CreateClient( TimeSpan? timeout = null ) =>
        new( "127.0.0.1", Port, timeout ?? TimeSpan.FromSeconds( 1 ) );

    /// <summary>
    /// Builds the frame the client should send for a command id and its arguments.
    /// </summary>
    public static byte[] Frame( params byte[] payload ) =>
        [0xFF, 0x00, 0x00, (byte) payload.Length, .. payload, Checksum( payload )];

    public void DropConnections()
    {
        lock( _connections )
            foreach( var connection in _connections )
                connection.Close();
    }

    private async Task AcceptLoop()
    {
        while( true )
        {
            TcpClient connection;
            try
            {
                connection = await _listener.AcceptTcpClientAsync( _cts.Token );
            }
            catch
            {
                return;
            }

            lock( _connections )
                _connections.Add( connection );
            _ = Serve( connection );
        }
    }

    private async Task Serve( TcpClient connection )
    {
        try
        {
            var stream = connection.GetStream();
            var buffer = new byte[256];
            while( true )
            {
                var count = await stream.ReadAsync( buffer, _cts.Token );
                if( count == 0 )
                    return;

                var frame = buffer[..count];
                Received.Enqueue( frame );

                if( Respond( frame ) is not { } response )
                    continue;

                await Task.Delay( ResponseDelay, _cts.Token );
                await stream.WriteAsync( (byte[]) [.. response, Checksum( response )], _cts.Token );
            }
        }
        catch
        {
            // Connection dropped by the client or the test
        }
    }

    private static byte Checksum( byte[] bytes )
    {
        byte sum = 0;
        foreach( var b in bytes )
            sum += b;
        return sum;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        DropConnections();
    }
}
