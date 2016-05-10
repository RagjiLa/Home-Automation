using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOTGatewayHost.Business_Logic
{
    public class RequestLogger : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Logger.Log(request.GetOwinContext().Request.RemoteIpAddress.ToString());
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}
