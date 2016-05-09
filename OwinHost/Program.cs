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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OwinHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + GetLocalIpAddress() + ":9000/";
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Controllers.dll"));
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DeviceControllers.dll"));

            Startup.BaseUrl = baseAddress;
            using (WebApp.Start<Startup>(baseAddress))
            {
                Console.WriteLine("Server Activated @" + baseAddress);

                //using (var target = new KeyValueTests(baseAddress))
                //{
                //    target.DeleteSingle();

                //    target.PostSingle();

                //    target.GetSingle();

                //    target.PutSingle();

                //    target.DeleteSingle();
                //}

                //using (var target = new InternetDashboardTests(baseAddress))
                //{
                //    target.Post();
                //}

                //using (var target = new TimeSeriesTests(baseAddress))
                //{
                //    target.PostMany();

                //    target.GetSingle();

                //    target.DeleteSingle();

                //    target.PostSingle();
                //}
                Console.WriteLine("Press enter to exit to shutdown server.");
                Console.ReadLine();
            }
            Console.WriteLine("Stopped press enter to exit application");
            Console.ReadLine();
        }

        public static IPAddress GetLocalIpAddress()
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
            config.EnsureInitialized();
            Console.WriteLine("Accepted routes:");
            foreach (var s in config.Services.GetApiExplorer().ApiDescriptions)
            {
                var apiString = s.HttpMethod.Method + "  " + BaseUrl + s.RelativePath;
                Console.WriteLine("     " + apiString);
            }
        }
    }

    public class TimeSeriesTests : HttpClient
    {
        public TimeSeriesTests(string baseUrl)
        {
            base.BaseAddress = new Uri(baseUrl);
        }

        public void PostMany()
        {
            var data = new Dictionary<string, string>();
            data.Add(DateTime.MinValue.ToBinary().ToString(), DateTime.MinValue.ToLongDateString());
            data.Add(DateTime.MaxValue.ToBinary().ToString(), DateTime.MaxValue.ToLongDateString());
            var jsonDatatoPost = new JavaScriptSerializer().Serialize(data);
            StringContent content = new StringContent(jsonDatatoPost, Encoding.UTF8, "application/json");
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = PostAsync("TimeSeries/Laukik", content).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public void PostSingle()
        {
            var data = new Dictionary<string, string>();
            var jsonDatatoPost = new JavaScriptSerializer().Serialize(data);
            StringContent content = new StringContent(jsonDatatoPost, Encoding.UTF8, "application/json");
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = PostAsync("TimeSeries/Laukik/" + DateTime.UtcNow.ToString("o") + "/MyValue", content).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public void GetSingle()
        {
            var result = GetAsync("TimeSeries/Laukik/" + DateTime.MinValue.ToBinary().ToString()).Result;
            var str = result.Content.ReadAsStringAsync().Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public void DeleteSingle()
        {
            var result = DeleteAsync("TimeSeries/Laukik/" + DateTime.MinValue.ToBinary().ToString()).Result;
            var str = result.Content.ReadAsStringAsync().Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }
    }

    public class InternetDashboardTests : HttpClient
    {
        public InternetDashboardTests(string baseUrl)
        {
            base.BaseAddress = new Uri(baseUrl);
        }

        public void Post()
        {
            var data = new JavaScriptSerializer().Serialize(new TestData() { Temperature = 52, Humidity = 52 });
            StringContent dummyContent = new StringContent("Laukik", Encoding.UTF8, "application/json");
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = PostAsync("InternetDashboard/Laukik9562/" + data, dummyContent).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public class TestData
        {
            public int Temperature { get; set; }
            public int Humidity { get; set; }
        }
    }

    public class KeyValueTests : HttpClient
    {
        public KeyValueTests(string baseUrl)
        {
            base.BaseAddress = new Uri(baseUrl);
        }

        public void PostSingle()
        {
            var data = new Dictionary<string, string>();
            data.Add(DateTime.MinValue.ToBinary().ToString(), DateTime.MinValue.ToLongDateString());
            data.Add(DateTime.MaxValue.ToBinary().ToString(), DateTime.MaxValue.ToLongDateString());
            var jsonDatatoPost = new JavaScriptSerializer().Serialize(data);
            StringContent content = new StringContent(jsonDatatoPost, Encoding.UTF8, "application/json");
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = PostAsync("KeyValuePair", content).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public void GetSingle()
        {
            var result = GetAsync("KeyValuePair/" + DateTime.MinValue.ToBinary().ToString()).Result;
            var str = result.Content.ReadAsStringAsync().Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public void DeleteSingle()
        {
            var result = DeleteAsync("KeyValuePair/" + DateTime.MinValue.ToBinary().ToString()).Result;
            var str = result.Content.ReadAsStringAsync().Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }

        public void PutSingle()
        {
            StringContent dummyContent = new StringContent("", Encoding.UTF8, "application/json");
            var result = PutAsync("KeyValuePair/Update/" + DateTime.MinValue.ToBinary().ToString() + "/Jan", dummyContent).Result;
            var str = result.Content.ReadAsStringAsync().Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }
    }
}
