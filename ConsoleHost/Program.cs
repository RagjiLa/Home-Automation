using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = new HttpClient())
            {
                //            var values = new Dictionary<string, string>
                //{
                //   { "thing1", "hello" },
                //   { "thing2", "world" }
                //};

                //            var content = new FormUrlEncodedContent(values);
                
                StringContent c = new StringContent(@"{""W"":""152""}", Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                // client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                var response = client.PostAsync("http://dweet.io/dweet/for/PlantManager442214Config", c).Result;

                var responseString = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(responseString);
            }
            Console.ReadLine();
        }
    }
}
