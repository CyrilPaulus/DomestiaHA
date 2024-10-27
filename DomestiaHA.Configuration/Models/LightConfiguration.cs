namespace DomestiaHA.Configuration.Models;

public class LightConfiguration
{
    public required string Label { get; set; }
    public bool Dimmable { get; set; }
    public int RelayId { get; set; }
}