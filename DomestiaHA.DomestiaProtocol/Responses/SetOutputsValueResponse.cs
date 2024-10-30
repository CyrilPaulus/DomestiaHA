using DomestiaHA.DomestiaProtocol.Commands;

namespace DomestiaHA.DomestiaProtocol.Responses;
internal class SetOutputsValueResponse : BaseDomestiaResponse<SetOutputsValueCommand>
{
    public override void Deserialize( byte[] data )
    {

    }
}
