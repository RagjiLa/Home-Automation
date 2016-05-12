using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Owin;
using System.Reflection;
using System.IO;
using Microsoft.Owin.Hosting;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;

namespace IOTGatewayHost.Business_Logic
{
    public class OwinHost : IDisposable
    {
        private IDisposable _owinObject = null;

        public string Start()
        {
            string baseAddress = "http://" + GetLocalIpAddress() + ":9000/";
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Controllers.dll"));
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DeviceControllers.dll"));
            Startup.BaseUrl = baseAddress;
            _owinObject = WebApp.Start<Startup>(baseAddress);
            Logger.Log("Server Active @ " + baseAddress);
            return baseAddress;
        }

        public void Stop()
        {
            if (_owinObject != null) _owinObject.Dispose();
        }

        private static IPAddress GetLocalIpAddress()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip;
                    }
                }
                throw new Exception("Local IP Address Not Found!");
            }
            else
            {
                throw new Exception("Network not connected");
            }
        }

        public class Startup
        {
            public static string BaseUrl { get; set; }

            public void Configuration(IAppBuilder appBuilder)
            {
                HttpConfiguration config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();
                appBuilder.UseWebApi(config);
                config.DependencyResolver = new DependencyResolver(BaseUrl);
                config.MessageHandlers.Add(new RequestLogger());

                var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Web/");
                var fileServerOptions = new FileServerOptions()
                {
                    EnableDefaultFiles = true,
                    EnableDirectoryBrowsing = false,
                    RequestPath = new PathString(string.Empty),
                    FileSystem = new PhysicalFileSystem(filePath)
                };
                fileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;
                appBuilder.UseFileServer(fileServerOptions);

                config.EnsureInitialized();
                Logger.Log("Accepted routes:");
                foreach (var s in config.Services.GetApiExplorer().ApiDescriptions)
                {
                    var apiString = s.HttpMethod.Method + "  " + BaseUrl + s.RelativePath;
                    Logger.Log("     " + apiString);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }


}
