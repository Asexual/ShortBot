using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShortBot.Url
{
    public class McAfeeShortener : IUrlShortener
    {
        private const string McAfeeInputUrl = "http://mcaf.ee/api/shorten";

        //{"status_code": "200", "data": {"url": "http://mcaf.ee/8d0c3", "long_url": "http://google.com/", "hash": "8d0c3"}, "status_txt": "OK"}

        public string Name => "mcaf.ee";

        public bool SupportsAlias => false;


        public async Task<string> ShortenUrl(IWebProxy proxy, string url, string alias = null)
        {
            using (var client = HttpHelper.CreateHttpClient(proxy))
            {
                var msg = new HttpRequestMessage(HttpMethod.Post, McAfeeInputUrl);
                msg.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                msg.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                msg.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "input_url", url }
                });

                var resp = await client.SendAsync(msg);
                var content = await resp.Content.ReadAsStringAsync();
                var obj = JObject.Parse(content);
                return obj["data"]["url"].Value<string>();
            }
        }
    }
}
