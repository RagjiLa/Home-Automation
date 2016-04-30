using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hub.Utilities
{
    public class DataParser
    {
        private static object _syncLockForParse = new object();
        public static Exception TryParse(IEnumerable<byte> data, out Dictionary<string, string> parsedData)
        {
            lock (_syncLockForParse)
            {
                var encoding = Encoding.UTF8;
                parsedData = new Dictionary<string, string>();
                Exception returnValue = null;
                try
                {
                    var stringData = encoding.GetString(data.ToArray());
                    foreach (var kvp in stringData.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kvpair = kvp.Split(":".ToArray());
                        parsedData.Add(kvpair[0], kvpair[1]);
                    }
                }
                catch (Exception ex)
                {
                    returnValue = ex;
                }
                return returnValue;
            }
        }
    }
}
