using DomestiaHA.Abstraction;
using DomestiaHA.Abstraction.Models;
using DomestiaHA.DomestiaProtocol.Commands;
using DomestiaHA.DomestiaProtocol.Enums;
using DomestiaHA.DomestiaProtocol.Responses;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DomestiaHA.DomestiaProtocol;

public class DomestiaLightServiceConfiguration
{
    public required string IpAddress { get; set; }
}

public class DomestiaLightService : ILightService, IDisposable
{
    private readonly ILogger<DomestiaLightService> _logger;
    private readonly DomestiaLightServiceConfiguration _options;
    private readonly DomestiaConnector _connector;

    private Dictionary<string, DomestiaRelayConfiguration> _relayConfigurations = new();

    public DomestiaLightService(
        ILogger<DomestiaLightService> logger,
        IOptions<DomestiaLightServiceConfiguration> options )
    {
        _options = options.Value;
        _connector = new DomestiaConnector();
        _logger = logger;
    }

    public async Task Connect()
    {
        await _connector.Connect( _options.IpAddress );

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

    public List<Light> GetLights()
    {
        return _relayConfigurations
            .Values
            .Select( x => new Light( x.Label, IsDimmable( x.RelayType ) ) )
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetAllBrightness()
    {
        var command = new GetOutputsValueCommand();
        var result = await _connector.ExecuteCommand<GetOutputsValueCommand, GetOutputsValueResponse>( command );
        if( result == null )
            throw new Exception( "Can't load lights..." );

        return result.OutputsValue
            .Join( _relayConfigurations.Values, x => x.Key, x => x.RelayId, ( ov, relay ) => new
            {
                Label = relay.Label,
                RelayType = relay.RelayType,
                Value = ov.Value
            } )
            .ToDictionary( x => x.Label, x => ConvertToBrightness( x.Value, IsDimmable( x.RelayType ) ) );
    }

    public async Task<int> GetBrightness( Light light )
    {
        var values = await GetAllBrightness();
        return values[light.Label];
    }

    public async Task SetBrightness( Light light, int brightness )
    {
        var relay = _relayConfigurations[light.Label];
        var isDimmable = IsDimmable( relay.RelayType );

        var outputValue = ConvertFromBrightness( brightness, isDimmable );

        if( isDimmable )
        {
            await SetOuputDimValue( relay.RelayId, outputValue );
            return;
        }

        var isOn = await GetBrightness( light ) >= 1;

        if( outputValue == 0 && isOn )
        {
            await SetOuputOff( relay.RelayId );
            return;
        }

        if( outputValue >= 1 && !isOn )
        {
            await ToggleOutput( relay.RelayId );
            return;
        }
    }

    private async Task<Dictionary<int, RelayType>> GetOutputTypes()
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

    private async Task ToggleOutput( int outputId )
    {
        var command = new ToggleOutputCommand( (byte) outputId );
        var result = await _connector.ExecuteCommand<ToggleOutputCommand, ToggleOutputResponse>( command );
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

    private bool IsDimmable( RelayType relayType )
    {
        return relayType switch
        {
            RelayType.DimmerContinue or RelayType.DimmerStop => true,
            _ => false
        };
    }

    private int ConvertToBrightness( byte value, bool isDimmable )
    {
        return (value, isDimmable) switch
        {
            (0, false ) => 0,
            ( > 0, false ) => 255,
            (_, true ) => (int) (value / 64.0 * 255)
        };
    }

    private byte ConvertFromBrightness( int value, bool isDimmable )
    {
        return (value, isDimmable) switch
        {
            (0, false ) => 0,
            ( > 0, false ) => 1,
            (_, true ) => (byte) (value / 255.0 * 64),
            _ => 0
        };
    }

    public void Dispose()
    {
        _connector.Dispose();
    }


}
