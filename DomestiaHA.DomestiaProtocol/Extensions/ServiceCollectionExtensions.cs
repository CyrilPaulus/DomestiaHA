using DomestiaHA.Abstraction;

using Microsoft.Extensions.DependencyInjection;

namespace DomestiaHA.DomestiaProtocol.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomestiaLightService(
        this IServiceCollection serviceCollection,
        Action<DomestiaLightServiceConfiguration> configureAction )
    {
        return serviceCollection
            .AddSingleton<ILightService, DomestiaLightService>()
            .Configure( configureAction );
    }
}
