using System.Text;
using System.Text.RegularExpressions;

namespace MyCompany.Transfers.Admin.Client.Helpers;

/// <summary>
/// Генерация Id из названия: транслитерация кириллицы в латиницу, оставляем только буквы и цифры.
/// </summary>
public static class SlugHelper
{
    private static readonly Dictionary<char, string> CyrillicToLatin = new()
    {
        {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"}, {'е', "e"}, {'ё', "e"}, {'ж', "zh"}, {'з', "z"},
        {'и', "i"}, {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"},
        {'с', "s"}, {'т', "t"}, {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "sch"},
        {'ъ', ""}, {'ы', "y"}, {'ь', ""}, {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
        {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"}, {'Е', "E"}, {'Ё', "E"}, {'Ж', "Zh"}, {'З', "Z"},
        {'И', "I"}, {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"}, {'О', "O"}, {'П', "P"}, {'Р', "R"},
        {'С', "S"}, {'Т', "T"}, {'У', "U"}, {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"}, {'Щ', "Sch"},
        {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""}, {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"}
    };

    /// <summary>
    /// Строит Id из названия: транслитерация кириллицы, только буквы и цифры (без пробелов и лишних символов).
    /// </summary>
    public static string IdFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        var sb = new StringBuilder(name.Length * 2);
        foreach (var c in name.Trim())
        {
            if (CyrillicToLatin.TryGetValue(c, out var lat))
                sb.Append(lat);
            else if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        var result = sb.ToString();
        if (result.Length == 0)
            return "";
        return Regex.Replace(result, @"[^a-zA-Z0-9]", "").Trim();
    }
}
