using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ShortBot.Ext
{
    public static class RandomExt
    {
        private static readonly Random rnd = new Random();
        public static T RandomChoice<T>(this IEnumerable<T> source)
        {
            var arr = source.ToArray();
            return arr[rnd.Next(arr.Length)];
        }
    }
}
