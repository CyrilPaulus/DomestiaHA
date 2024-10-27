using Microsoft.Extensions.DependencyInjection;

namespace DomestiaHA.Configuration.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomestiaHAConfiguration( this IServiceCollection serviceCollection, Action<DomesitaHAConfigurationServiceOptions> configureAction )
    {
        return serviceCollection
            .AddSingleton<IDomestiaHAConfigurationService, DomestiaConfigurationHAService>()
            .Configure<DomesitaHAConfigurationServiceOptions>( configureAction );
    }
}
