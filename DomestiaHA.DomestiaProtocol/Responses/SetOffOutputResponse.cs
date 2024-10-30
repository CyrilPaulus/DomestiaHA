using DomestiaHA.DomestiaProtocol.Commands;

namespace DomestiaHA.DomestiaProtocol.Responses;
internal class SetOffOutputResponse : BaseDomestiaResponse<SetOffOutputCommand>
{
    public override void Deserialize( byte[] data )
    {

    }
}
