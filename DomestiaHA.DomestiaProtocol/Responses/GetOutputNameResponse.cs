using DomestiaHA.DomestiaProtocol.Commands;

namespace DomestiaHA.DomestiaProtocol.Responses;
public class GetOutputNameResponse : BaseDomestiaResponse<GetOutputNameCommand>
{
    public string OutputName { get; private set; } = string.Empty;

    public override void Deserialize( byte[] data )
    {
        var readAll256Bytes = data[2] == 1;
        var bytesCount = data[3];
        OutputName = ProtocolHelper.DecodeString( data.Skip( 4 ).ToArray() );
    }
}
