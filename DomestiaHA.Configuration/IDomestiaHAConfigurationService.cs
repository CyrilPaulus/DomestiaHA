using DomestiaHA.Configuration.Models;

namespace DomestiaHA.Configuration;
public interface IDomestiaHAConfigurationService
{
    public IEnumerable<LightConfiguration> GetLightConfigurations();
    MQTTConfiguration GetMQTTConfiguration();
}

public class DomesitaHAConfigurationServiceOptions
{
    public required string ConfigurationFile { get; set; }
}
