using System.Text;

namespace DomestiaHA.DomestiaProtocol;
internal static class ProtocolHelper
{
    public static string DecodeString( byte[] array )
    {
        var endIndex = Array.IndexOf( array, (byte) 255 );
        var length = endIndex > 0 ? endIndex : array.Length;
        return Encoding.ASCII.GetString( array, 0, length );
    }
}
