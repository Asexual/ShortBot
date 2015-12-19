
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace ShortBot.Url
{
    public class TinyCCShortener : IUrlShortener
    {
        private const string TinyCCUrl = "https://tiny.cc/ajax/create";

        public string Name => "tiny.cc";
        public bool SupportsAlias => true;

        //{"status":1,"message":"tinyd, packed-up and sent down... to Recent URLs area!","li":"<li class=\"selected\"><a href=\"javascript:;\">tiny.cc\/7xr96x<\/a><\/li>","tiny":"7xr96x"}
        public async Task<string> ShortenUrl(IWebProxy proxy, string url, string alias = null)
        {
            using (var client = HttpHelper.CreateHttpClient(proxy))
            {
                var csrfResponse = await client.GetAsync("https://tiny.cc/");
                var csrfContent = await csrfResponse.Content.ReadAsStringAsync();

                var csrfDoc = new HtmlDocument();
                csrfDoc.LoadHtml(csrfContent);

                var form = csrfDoc.DocumentNode.SelectSingleNode("//form[@id='create']");
                var urlName = form.SelectSingleNode("//input[starts-with(@name,'url_')]")
                    .GetAttributeValue("name", null);
                var aliasName = form.SelectSingleNode("//input[starts-with(@name, 'custom_')]")
                    .GetAttributeValue("name", null);
                var unknownField =
                    form.SelectSingleNode("//input[starts-with(@name, 'field_')]").GetAttributeValue("name", null);

                var referrer = form.SelectSingleNode("//input[@name='referrer']").GetAttributeValue("value", null);

                if (string.IsNullOrWhiteSpace(urlName) || string.IsNullOrWhiteSpace(aliasName) ||
                    string.IsNullOrWhiteSpace(unknownField) || string.IsNullOrWhiteSpace(referrer))
                    throw new UrlShortenException("Unable to find correct Tiny.cc fields.");

                var message = new HttpRequestMessage(HttpMethod.Post, TinyCCUrl);
                message.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                message.Headers.TryAddWithoutValidation("Content-Type",
                    "application/x-www-form-urlencoded; charset=UTF-8");
                message.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { urlName, url },
                    { aliasName, alias?? "customurl" },
                    { unknownField, "" },
                    { "referrer", referrer}
                });

                var response = await client.SendAsync(message);
                var content = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(content);
                if (obj["status"].Value<int>() != 1)
                    throw new UrlShortenException("Tiny.cc status non-one. Error occured.");

                return "http://tiny.cc/" + obj["tiny"].Value<string>();
            }
        }
    }
}
