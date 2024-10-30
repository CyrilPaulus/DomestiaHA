
namespace DomestiaHA.DomestiaProtocol.Commands;
internal class SetOffOutputCommand : BaseDomestiaCommand
{
    public override byte CommandId => 15;

    public override byte ParamsCount => 1;

    public byte OutputId { get; }

    public SetOffOutputCommand( byte outputId )
    {
        OutputId = outputId;
    }

    public override void Serialize( Stream stream )
    {
        stream.WriteByte( OutputId );
    }
}
