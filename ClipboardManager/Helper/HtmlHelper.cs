using System.Text.RegularExpressions;

public static class HtmlHelper
{
    public static MatchCollection GetUrlsFromText(string text)
    {
        string pattern = @"((http|https):\/\/[^\s]+)";
        MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.Multiline);
        return matches;
    }
}
