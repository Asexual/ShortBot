using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShortBot
{
    public class ProxyPool
    {
        private readonly List<Tuple<DateTime, IWebProxy>> _proxies; 
        private readonly object _sync;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public int Count => _proxies.Count;

        public ProxyPool()
        {
            _sync = new object();
            _proxies = new List<Tuple<DateTime, IWebProxy>>();
        }

        public ProxyPool(IEnumerable<IWebProxy> proxies) : this()
        {
            foreach (var proxy in proxies)
            {
                _proxies.Add(new Tuple<DateTime, IWebProxy>(DateTime.Now, proxy));
            }
        }

        public void Add(IWebProxy proxy)
        {
            lock (_sync)
            {
                _proxies.Add(new Tuple<DateTime, IWebProxy>(DateTime.Now, proxy));
                _proxies.Sort((x, y) => DateTime.Compare(x.Item1, y.Item1));
            }
        }

        public void Remove(IWebProxy proxy)
        {
            lock (_sync)
            {
                _proxies.RemoveAt(_proxies.FindIndex(a => a.Item2 == proxy));
            }
        }

        public IWebProxy GetOldestProxy()
        {
            lock (_sync)
            {
                var proxy = _proxies.First();
                proxy = new Tuple<DateTime, IWebProxy>(DateTime.Now, proxy.Item2);
                _proxies.Add(proxy);
                _proxies.Sort((x, y) => DateTime.Compare(x.Item1, y.Item1));
                return proxy.Item2;
            }
        }
    }
}
