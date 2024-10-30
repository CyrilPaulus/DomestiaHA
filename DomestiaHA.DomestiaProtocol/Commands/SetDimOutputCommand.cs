
namespace DomestiaHA.DomestiaProtocol.Commands;
internal class SetDimOutputCommand : BaseDomestiaCommand
{
    public override byte CommandId => 16;

    public override byte ParamsCount => 2;

    public byte OutputId { get; }
    public byte DimValue { get; }

    public SetDimOutputCommand( byte outputId, byte dimValue )
    {
        OutputId = outputId;
        DimValue = dimValue;
    }

    public override void Serialize( Stream stream )
    {
        stream.WriteByte( OutputId );
        stream.WriteByte( DimValue );
    }
}
