using System;
using System.Text;

namespace Hub
{
    public class PluginName
    {
        public static PluginName DweetPlugin { get { return new PluginName("DP"); } }
        public static PluginName PlantManagerPlugin { get { return new PluginName("PM"); } }

        private readonly string _name;

        private PluginName(string Name)
        {
            _name = Name;
        }

        public override string ToString()
        {
            return _name.ToLower();
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(this.ToString());
        }
    }
}
