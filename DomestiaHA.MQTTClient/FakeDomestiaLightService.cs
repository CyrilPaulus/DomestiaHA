using DomestiaHA.Abstraction;

namespace DomestiaHA.MQTTClient;

internal class FakeDomestiaLightService : IDomestiaLightService
{

    private readonly Dictionary<string, int> _lightBrigthnessValues = [];

    public int GetBrigthness( string lightId )
    {
        return _lightBrigthnessValues.GetValueOrDefault( lightId );
    }

    public void SetBrigthness( string lightId, int brigthness )
    {
        _lightBrigthnessValues[lightId] = brigthness;
    }
}