using DomestiaHA.DomestiaProtocol.Commands;
using DomestiaHA.DomestiaProtocol.Enums;

namespace DomestiaHA.DomestiaProtocol.Responses;
public class GetOutputTypeResponse : BaseDomestiaResponse<GetOutputsTypeCommand>
{
    public Dictionary<int, RelayType> OutputTypes { get; private set; } = new();

    public override void Deserialize( byte[] data )
    {
        var count = data[3];
        for( int i = 0; i < count; i++ )
        {
            OutputTypes[i + 1] = (RelayType) data[i + 4];
        }
    }
}
