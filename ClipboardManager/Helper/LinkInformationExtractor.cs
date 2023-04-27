using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Models;

public static class LinkInformationExtractor
{
    static string defaultImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Images/noImage.png");
    public static async Task<UrlModel> GetLinkInformationAsync(string url)
    {
        var result = new UrlModel();
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            MessageBox.Show("Invalid URL: " + url);
            return null;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            MessageBox.Show("Unsupported URI scheme: " + uri.Scheme);
            return null;
        }

        var web = new HtmlWeb();
        try
        {
            var doc = await web.LoadFromWebAsync(url);
            var imageUrl = doc.DocumentNode.SelectSingleNode("//head/meta[@property='og:image']")?.GetAttributeValue("content", "");
            
            result = new UrlModel
            {
                Url = url,
                Title = doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText ?? string.Empty,
                Description = doc.DocumentNode.SelectSingleNode("//head/meta[@name='description']")?.GetAttributeValue("content", "") ?? string.Empty,
                ImageUrl = imageUrl ?? defaultImage,
            };

        }
        catch (HtmlWebException ex)
        {
            MessageBox.Show("Error loading web page: " + ex.Message);
            return null;
        }

        return result;
    }
}