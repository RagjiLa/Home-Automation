using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayKernel
{
    public interface ISingleSessionPlugin
    {
        string Name { get; }
        IEnumerable<byte> Respond(IEnumerable<byte> data);
        void PostResponseProcess(IEnumerable<byte> requestData, IEnumerable<byte> responseData);
    }
   
}
