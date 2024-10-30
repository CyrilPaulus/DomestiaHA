using DomestiaHA.DomestiaProtocol.Commands;

namespace DomestiaHA.DomestiaProtocol.Responses;
internal class GetOutputsValueResponse : BaseDomestiaResponse<GetOutputsValueCommand>
{
    public Dictionary<int, byte> OutputsValue { get; } = new();

    public override void Deserialize( byte[] data )
    {
        for( var i = 3; i < data.Length; i++ )
        {
            OutputsValue[i - 2] = data[i];
        }
    }
}
