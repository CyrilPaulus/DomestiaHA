namespace DomestiaHA.DomestiaProtocol.Commands;
internal class GetOutputsValueCommand : BaseDomestiaCommand
{
    public override byte CommandId => 60;

    public override byte ParamsCount => 0;

    public override void Serialize( Stream stream )
    {

    }
}
