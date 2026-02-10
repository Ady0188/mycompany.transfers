using System.Text;

namespace MyCompany.Transfers.Api.Helpers;

internal class Windows1251EncodingProvider : EncodingProvider
{
    public override Encoding GetEncoding(string name)
    {
        if (string.Equals(name, "windows-1251", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.GetEncoding(1251); // 1251 is the code page for Windows-1251 encoding
        }
        return null; // Return null for unsupported encodings
    }

    public override Encoding GetEncoding(int codepage)
    {
        if (codepage == 1251)
        {
            return Encoding.GetEncoding(1251); // 1251 is the code page for Windows-1251 encoding
        }
        return null; // Return null for unsupported encodings
    }
}