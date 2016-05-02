using System.Collections.Generic;
using System.Text;

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
            return DataParser.FromKeyValuePairs(sample.ToKeyValuePair());
        }

        public static void FromByteArray(this ISample sample, IEnumerable<byte> byteData)
        {
            var kvpData = DataParser.ToKeyValuePairs(byteData);
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
