using DomestiaHA.DomestiaProtocol.Commands;

namespace DomestiaHA.DomestiaProtocol.Responses;
public abstract class BaseDomestiaResponse<T>
    where T : BaseDomestiaCommand
{
    public abstract void Deserialize( byte[] data );
}
