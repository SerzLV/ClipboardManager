using System.Text.RegularExpressions;

public static class HtmlHelper
{
    public static MatchCollection GetUrlsFromText(string text)
    {
        string pattern = @"\b(?:https?://|www\.)\S+\b";
        MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.Multiline);
        return matches;
    }
}
