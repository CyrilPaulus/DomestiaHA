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
    /// Set a light brightness value
    /// </summary>
    /// <param name="lightId">lightId</param>
    /// <param name="brightness">brightness value [0, 255]</param>
    public Task SetBrightness( Light light, int brightness );

    /// <summary>
    /// Get a light brightness value
    /// </summary>
    /// <param name="lightId">lightId</param>
    /// <returns>brightness [0,255]</returns>
    public Task<int> GetBrightness( Light light );

    /// <summary>
    /// Get all light brightness in one command.
    /// </summary>
    /// <returns>All light brightness [0,255]</returns>
    public Task<Dictionary<string, int>> GetAllBrightness();

    public Task Connect();

}