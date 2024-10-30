
namespace DomestiaHA.DomestiaProtocol.Commands;
public class GetOutputNameCommand : BaseDomestiaCommand
{
    public override byte CommandId => 62;

    public override byte ParamsCount => 1;

    public byte OutputIndex { get; }

    public GetOutputNameCommand( byte outputIndex )
    {
        OutputIndex = outputIndex;
    }

    public override void Serialize( Stream stream )
    {
        stream.WriteByte( OutputIndex );
    }
}
