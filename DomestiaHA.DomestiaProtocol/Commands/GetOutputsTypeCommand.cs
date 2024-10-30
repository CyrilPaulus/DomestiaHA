namespace DomestiaHA.DomestiaProtocol.Commands;
public class GetOutputsTypeCommand : BaseDomestiaCommand
{
    public override byte CommandId => 66;

    public override byte ParamsCount => 0;

    public override void Serialize( Stream stream )
    {

    }
}
