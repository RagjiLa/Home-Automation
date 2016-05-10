using IOTGatewayHost.Business_Logic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IOTGatewayHost
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private OwinHost _apiHost = null;
        public OwinHost ApiHost
        {
            get
            {
                if (_apiHost == null) _apiHost = new OwinHost();
                return _apiHost;
            }
        }
    }
}
