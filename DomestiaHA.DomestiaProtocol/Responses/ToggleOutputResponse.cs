using DomestiaHA.DomestiaProtocol.Commands;

namespace DomestiaHA.DomestiaProtocol.Responses;
internal class ToggleOutputResponse : BaseDomestiaResponse<ToggleOutputCommand>
{
    public bool Success { get; private set; }

    public override void Deserialize( byte[] data )
    {
        Success = true;
    }
}
