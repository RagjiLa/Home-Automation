using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayKernel
{
    public interface IMultiSessionPlugin : ISingleSessionPlugin
    {
        IMultiSessionPlugin Clone();
    }
}
