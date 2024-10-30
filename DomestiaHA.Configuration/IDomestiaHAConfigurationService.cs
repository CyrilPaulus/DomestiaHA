using DomestiaHA.Configuration.Models;

namespace DomestiaHA.Configuration;
public interface IDomestiaHAConfigurationService
{
    public IEnumerable<LightConfiguration> GetLightConfigurations();
    MQTTConfiguration GetMQTTConfiguration();
    DomestiaConfiguration GetDomestiaConfiguration();
}

public class DomesitaHAConfigurationServiceOptions
{
    public required string ConfigurationFile { get; set; }
}
