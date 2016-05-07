using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Microsoft.Owin.Hosting;
using Owin;
using System.Web.Script.Serialization;
using Controllers;
using System.Data;
using System.Web.Http.Dependencies;

namespace OwinHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9000/";
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Controllers.dll"));
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DeviceControllers.dll"));

            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("Server Activated @" + baseAddress);

                #region TimeseriesController

                var target = new TimeSeriesController();
                target.Post(baseAddress + "api/TimeSeries");


                //using (var client = new HttpClient())
                //{
                //    var result = client.GetAsync(baseAddress + "api/TimeSeries/50").Result;

                //    var str = result.Content.ReadAsStringAsync().Result;
                //    var t = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(str);
                //    Console.WriteLine(result);

                //}

                #endregion StoreController

                #region InternetDashboardController

                using (var client = new HttpClient())
                {
                    var jsonDatatoPost = new JavaScriptSerializer().Serialize(new InternetDashboardController.DweetSample() { ResourceName = "Laukik9562", Data = new JavaScriptSerializer().Serialize(new g() { Temperature = 52, Humidity = 52 }) });
                    StringContent content = new StringContent(jsonDatatoPost, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var result = client.PostAsync(baseAddress + "api/InternetDashboard", content).Result;
                    Console.WriteLine(result);
                }

                #endregion InternetDashboardController

                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
            Console.WriteLine("Stopped press enter to exit application");
            Console.ReadLine();
        }
    }

    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            config.Routes.MapHttpRoute(name: "DefaultApi", routeTemplate: "api/{controller}/");
            //    config.Routes.MapHttpRoute(name: "ApiById", routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional },
            //    constraints: new { id = @"^[0-9]+$" }
            //);
            //config.DependencyResolver = new g();
            Console.WriteLine(config.VirtualPathRoot);
            foreach (var s in config.Services.GetApiExplorer().ApiDescriptions)
            {
                var apiString = s.HttpMethod.Method + "  " + s.RelativePath;
                Console.WriteLine(apiString);
            }
            appBuilder.UseWebApi(config);
        }
    }

    public class g : IDependencyResolver
    {
        public int Temperature { get; set; }
        public int Humidity { get; set; }
        public IDependencyScope BeginScope()
        {
            return new g();
        }

        public object GetService(Type serviceType)
        {
            var x = new TimeSeriesController();
            if (x.GetType() == serviceType)
            {
                return x;
            }
            Console.WriteLine(serviceType);
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return new List<object>();
        }

        public void Dispose()
        {
            Console.WriteLine("Disposed");
        }
    }

    public class TimeSeriesController
    {
        public void Post(string url)
        {
            using (var client = new HttpClient())
            {
                var data = new Dictionary<string, string>();
                data.Add(DateTime.MinValue.ToBinary().ToString(), DateTime.MinValue.ToLongDateString());
                data.Add(DateTime.MaxValue.ToBinary().ToString(), DateTime.MaxValue.ToLongDateString());
                var jsonDatatoPost = new JavaScriptSerializer().Serialize(data);
                StringContent content = new StringContent(jsonDatatoPost, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var result = client.PostAsync(url + "?uniqueName=Laukik", content).Result;
                if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
                Console.WriteLine(result);
            }
        }
    }
}
