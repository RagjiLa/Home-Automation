using System.Collections.Generic;
using System.Text;

namespace Kernel
{
    public class PluginName
    {
        public static PluginName DweetPlugin { get { return new PluginName("DP"); } }
        public static PluginName PlantManagerPlugin { get { return new PluginName("PM"); } }
        public static PluginName SqLitePlugin { get { return new PluginName("SQ"); } }

        private readonly string _name;

        private PluginName(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name.ToLower();
        }

        public IEnumerable<byte> ToBytes()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }
}
