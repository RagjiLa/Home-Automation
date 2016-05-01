using Hub.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hub
{
    public interface ISample
    {
        IDictionary<string, string> ToKeyValuePair();
        void FromKeyValuePair(IDictionary<string, string> kvpData);
    }
    public static class SampleExtensions
    {
        public static IEnumerable<byte> ToByteArray(this ISample sample)
        {
            IEnumerable<byte> byteArraydata;

            var exception = DataParser.TryConvert(sample.ToKeyValuePair(), out byteArraydata);
            if (exception != null) throw exception;

            return byteArraydata;
        }

        public static void FromByteArray(this ISample sample, IEnumerable<byte> byteData)
        {
            IDictionary<string, string> kvpData;

            var exception = DataParser.TryParse(byteData, out kvpData);
            if (exception != null) throw exception;

            sample.FromKeyValuePair(kvpData);
        }

        public static string ToJsonString(this ISample sample)
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{");
            foreach (var kvp in sample.ToKeyValuePair())
            {
                jsonString.Append("\"");
                jsonString.Append(kvp.Key);
                jsonString.Append("\"");
                jsonString.Append(":");
                jsonString.Append("\"");
                jsonString.Append(kvp.Value);
                jsonString.Append("\"");
                jsonString.Append(",");
            }
            jsonString.Remove(jsonString.Length - 1, 1);
            jsonString.Append("}");
            return jsonString.ToString();
        }
    }
}
