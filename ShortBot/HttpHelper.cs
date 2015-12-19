using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ShortBot.Ext;

namespace ShortBot
{
    public static class HttpHelper
    {
        private static readonly List<string> UserAgents = new List<string>();

        static HttpHelper()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ShortBot.UserAgents.txt");
            if (stream == null)
            {
                UserAgents.Add(
                    "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");
                UserAgents.Add(
                    "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");
                return;
            }
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    UserAgents.Add(line);
                }
            }
        } 

        public static HttpClient CreateHttpClient(IWebProxy proxy=null)
        {
            var client = new HttpClient(new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = false,
                UseProxy = proxy != null,
                Proxy = proxy
            });
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", RandomUserAgent());
            return client;
        }

        public static string RandomUserAgent()
        {
            return UserAgents.RandomChoice();
        }
    }
}
