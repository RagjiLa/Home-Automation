using GatewayKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayPlugins
{
    public class PlantMangerPlugin : ISingleSessionPlugin
    {
        public string Name
        {
            get
            {
                return "PlantManager";
            }
        }

        public void PostResponseProcess(IEnumerable<byte> requestData, IEnumerable<byte> responseData)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte> Respond(IEnumerable<byte> data)
        {
            throw new NotImplementedException();
        }
    }
}
