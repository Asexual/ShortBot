using System;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ShortBot.Url
{
    public class TinyUrlShortener : IUrlShortener
    {

        private const string TinyUrlCreatePage =
            "https://tinyurl.com/create.php?source=indexpage&url={0}&submit=Make+TinyURL%21&alias={1}";

        public string Name => "tinyurl.com";

        public bool SupportsAlias => true;

        public async Task<string> ShortenUrl(IWebProxy proxy, string url, string alias = null)
        {
            using (var client = HttpHelper.CreateHttpClient(proxy))
            {
                var createUrl = string.Format(TinyUrlCreatePage, url, alias ?? "");
                var result = await client.GetAsync(createUrl);
                var content = await result.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                if (!content.Contains("was created!"))
                    throw new UrlShortenException("TinyUrl did not create a shortened URL.");

                var b = doc.DocumentNode.SelectSingleNode("//div[@class='indent']/b");
                return b.InnerText;
            }
        }

    }
}
