using System;
using System.Net;
using System.Net.Configuration;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ShortBot.Url
{
    public class UrlShortenException : Exception
    {
        public UrlShortenException(string msg) : base(msg)
        {
            
        }
    }

    public interface IUrlShortener
    {
        string Name { get; }
        
        bool SupportsAlias { get; }

        Task<string> ShortenUrl(IWebProxy proxy, string url, string alias = null);
    }
}
