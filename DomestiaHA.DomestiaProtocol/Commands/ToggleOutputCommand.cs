
namespace DomestiaHA.DomestiaProtocol.Commands;
internal class ToggleOutputCommand : BaseDomestiaCommand
{
    public override byte CommandId => 13;

    public override byte ParamsCount => 1;

    public byte OutputId { get; }

    public ToggleOutputCommand( byte outputId )
    {
        OutputId = outputId;
    }

    public override void Serialize( Stream stream )
    {
        stream.WriteByte( OutputId );
    }
}
