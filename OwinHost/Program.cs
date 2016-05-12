using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Web.Script.Serialization;

namespace OwinHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + GetLocalIpAddress() + ":9000/";

            Console.WriteLine("Connecting @" + baseAddress);

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

            using (var target = new TimeSeriesTests(baseAddress))
            {
                target.PostMany("Laukik", 100);

                //target.GetSingle();

                //target.DeleteSingle();

                //target.PostSingle();
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



    public class TimeSeriesTests : HttpClient
    {
        public TimeSeriesTests(string baseUrl)
        {
            base.BaseAddress = new Uri(baseUrl);
        }

        public void PostMany(string deviceName, int count)
        {
            var data = new Dictionary<string, string>();
            for (int dataCtr = 0; dataCtr < count; dataCtr++)
                data.Add(DateTime.Now.AddDays(dataCtr).ToUniversalTime().ToString("O"), dataCtr.ToString());

            var jsonDatatoPost = new JavaScriptSerializer().Serialize(data);
            StringContent content = new StringContent(jsonDatatoPost, Encoding.UTF8, "application/json");
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = PostAsync("TimeSeries/" + deviceName, content).Result;
            if (result.StatusCode != HttpStatusCode.OK) throw new Exception("Failed");
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
