
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using DomestiaHA.Configuration.Models;

using Microsoft.Extensions.Options;

namespace DomestiaHA.Configuration;
internal class DomestiaConfigurationHAService : IDomestiaHAConfigurationService
{
    private DomestiaHAConfiguration _configuration;

    public DomestiaConfigurationHAService( IOptions<DomesitaHAConfigurationServiceOptions> options )
    {
        LoadConfiguration( options.Value.ConfigurationFile );
    }

    public DomestiaConfiguration GetDomestiaConfiguration()
    {
        return _configuration.Domestia;
    }

    public IEnumerable<LightConfiguration> GetLightConfigurations()
    {
        return _configuration.Lights;
    }

    public MQTTConfiguration GetMQTTConfiguration()
    {
        return _configuration.MQTT;
    }

    [MemberNotNull( nameof( _configuration ) )]
    private void LoadConfiguration( string configurationFilePath )
    {
        var configStr = File.ReadAllText( configurationFilePath );
        var config = JsonSerializer.Deserialize<DomestiaHAConfiguration>( configStr );
        if( config is null )
            throw new Exception( "Invalid configuration" );

        _configuration = config;
    }
}
