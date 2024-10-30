using DomestiaHA.DomestiaProtocol.Commands;
using DomestiaHA.DomestiaProtocol.Enums;
using DomestiaHA.DomestiaProtocol.Responses;

using Microsoft.Extensions.Logging;

namespace DomestiaHA.DomestiaProtocol;
public class DomestiaService : IDisposable
{
    private readonly ILogger<DomestiaService> _logger;

    private readonly DomestiaConnector _connector;
    private Dictionary<string, DomestiaRelayConfiguration> _relayConfigurations = new();

    public DomestiaService( ILogger<DomestiaService> logger )
    {
        _connector = new DomestiaConnector();
        _logger = logger;
    }

    public async Task Connect( string ip )
    {
        await _connector.Connect( ip );

        _logger.LogInformation( "Retrieving domestia configuration" );

        var outputTypes = await GetOutputTypes();
        foreach( var outputType in outputTypes )
        {
            if( outputType.Value == RelayType.Unused )
                continue;

            var outputName = await GetOutputName( outputType.Key );
            _relayConfigurations.Add( outputName, new DomestiaRelayConfiguration
                (
                RelayId: outputType.Key,
                RelayType: outputType.Value,
                Label: outputName
                ) );
        }

    }

    public Dictionary<string, DomestiaRelayConfiguration> GetRelayConfigurations()
    {
        return _relayConfigurations;
    }

    public async Task<Dictionary<string, byte>> GetOutputValues()
    {
        var command = new GetOutputsValueCommand();
        var result = await _connector.ExecuteCommand<GetOutputsValueCommand, GetOutputsValueResponse>( command );

        return result.OutputsValue
            .Join( _relayConfigurations.Values, x => x.Key, x => x.RelayId, ( ov, relay ) => new
            {
                Label = relay.Label,
                Value = ov.Value
            } )
            .ToDictionary( x => x.Label, x => x.Value );
    }

    public async Task<byte> GetOutputValue( string label )
    {
        var values = await GetOutputValues();
        return values[label];
    }

    public async Task SetOutputValue( string label, byte value )
    {
        var relay = _relayConfigurations[label];

        if( relay.RelayType == RelayType.DimmerContinue || relay.RelayType == RelayType.DimmerStop )
        {
            await SetOuputDimValue( relay.RelayId, value );
            return;
        }

        var isOn = await GetOutputValue( label ) >= 1;

        if( value == 0 && isOn )
        {
            await SetOuputOff( relay.RelayId );
            return;
        }

        if( value >= 1 && !isOn )
        {
            await ToggleOutput( relay.RelayId );
            return;
        }
    }

    public async Task<Dictionary<int, RelayType>> GetOutputTypes()
    {
        var command = new GetOutputsTypeCommand();
        var result = await _connector.ExecuteCommand<GetOutputsTypeCommand, GetOutputTypeResponse>( command );
        if( result == null )
            throw new Exception();

        return result.OutputTypes;
    }

    private async Task<string> GetOutputName( int outputId )
    {
        var command = new GetOutputNameCommand( (byte) outputId );
        var result = await _connector.ExecuteCommand<GetOutputNameCommand, GetOutputNameResponse>( command );
        if( result == null )
            throw new Exception();

        return result.OutputName;
    }

    private async Task<bool> ToggleOutput( int outputId )
    {
        var command = new ToggleOutputCommand( (byte) outputId );
        var result = await _connector.ExecuteCommand<ToggleOutputCommand, ToggleOutputResponse>( command );
        return result.Success;
    }

    private async Task SetOuputOff( int outputId )
    {
        var command = new SetOffOutputCommand( (byte) outputId );
        var result = await _connector.ExecuteCommand<SetOffOutputCommand, SetOffOutputResponse>( command );
    }

    private async Task SetOuputDimValue( int outputId, byte dimValue )
    {
        var command = new SetDimOutputCommand( (byte) outputId, dimValue );
        var result = await _connector.ExecuteCommand<SetDimOutputCommand, SetDimOutputResponse>( command );
    }



    public void Dispose()
    {
        _connector.Dispose();
    }
}
