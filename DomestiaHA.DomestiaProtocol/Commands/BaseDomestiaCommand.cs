namespace DomestiaHA.DomestiaProtocol.Commands;

public abstract class BaseDomestiaCommand
{
    public abstract byte CommandId { get; }
    public abstract byte ParamsCount { get; }

    public abstract void Serialize( Stream stream );

}
