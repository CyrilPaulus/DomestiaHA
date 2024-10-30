
namespace DomestiaHA.DomestiaProtocol.Commands;
internal class SetOutputsValueCommand : BaseDomestiaCommand
{
    private readonly Dictionary<byte, int> _values = new();

    public override byte CommandId => 150;

    public override byte ParamsCount => (byte) (_values.Count * 2);

    public override void Serialize( Stream stream )
    {
        foreach( var kvp in _values )
        {
            stream.WriteByte( kvp.Key );
            stream.WriteByte( (byte) kvp.Value );
        }

    }

    public void AddValue( byte relay, int value )
    {
        _values[relay] = value;
    }
}
