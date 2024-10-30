
namespace DomestiaHA.DomestiaProtocol.Commands;
internal class SetOnOutputCommand : BaseDomestiaCommand
{
    public override byte CommandId => 14;

    public override byte ParamsCount => 1;

    public byte OutputId { get; }

    public SetOnOutputCommand( byte outputId )
    {
        OutputId = outputId;
    }

    public override void Serialize( Stream stream )
    {
        stream.WriteByte( OutputId );
    }
}
