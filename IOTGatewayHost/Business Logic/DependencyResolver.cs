using ControllerProxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace IOTGatewayHost.Business_Logic
{
    public class DependencyResolver : IDependencyResolver
    {
        public string BaseUrl { get; set; }
        public DependencyResolver(string baseUrl)
        {
            BaseUrl = baseUrl;
        }
        public IDependencyScope BeginScope()
        {
            return new DependencyResolver(BaseUrl);
        }

        public void Dispose()
        {

        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(TimeSeriesProxy))
            {
                return new TimeSeriesProxy(BaseUrl);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return new List<object>();
        }
    }
}
