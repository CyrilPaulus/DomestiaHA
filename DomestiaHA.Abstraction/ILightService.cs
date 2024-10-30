using DomestiaHA.Abstraction.Models;

namespace DomestiaHA.Abstraction;

public interface ILightService
{

    /// <summary>
    /// Returns all light available in the system
    /// </summary>
    /// <returns></returns>
    public List<Light> GetLights();

    /// <summary>
    /// Set a light brigthness value
    /// </summary>
    /// <param name="lightId">lightId</param>
    /// <param name="brigthness">brightness value [0, 255]</param>
    public Task SetBrigthness( Light light, int brigthness );

    /// <summary>
    /// Get a light brigthness value
    /// </summary>
    /// <param name="lightId">lightId</param>
    /// <returns>brigthness [0,255]</returns>
    public Task<int> GetBrigthness( Light light );

    /// <summary>
    /// Get all light brigthness in one command.
    /// </summary>
    /// <returns>All ligth brigthness [0,255]</returns>
    public Task<Dictionary<string, int>> GetAllBrigthness();

    public Task Connect();

}