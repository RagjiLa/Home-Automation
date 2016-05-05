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

namespace OwinHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9000/";
            AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Controllers.dll"));
            // Start OWIN host 
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                // Create HttpCient and make a request to api/values 
                HttpClient client = new HttpClient();

                var response = client.GetAsync(baseAddress + "api/values").Result;

                Console.WriteLine(response);
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                Console.ReadLine(); 
            }
            
            Console.ReadLine(); 
        }

        public class MyNewAssembliesResolver : DefaultAssembliesResolver
        {
            public virtual ICollection<Assembly> GetAssemblies()
            {

                ICollection<Assembly> baseAssemblies = base.GetAssemblies();
                List<Assembly> assemblies = new List<Assembly>(baseAssemblies);
                var controllersAssembly = Assembly.LoadFrom(Environment.CurrentDirectory);
                baseAssemblies.Add(controllersAssembly);
                return baseAssemblies;

            }
        }
    }

    //public class ValuesController : ApiController
    //{
    //    // GET api/values 
    //    public IEnumerable<string> Get()
    //    {
    //        return new string[] { "value1", "value2" };
    //    }

    //    // GET api/values/5 
    //    public string Get(int id)
    //    {
    //        return "value";
    //    }

    //    // POST api/values 
    //    public IHttpActionResult Post([FromBody]string value)
    //    {
    //        return Ok<string>("Laukik");
    //    }

    //    // PUT api/values/5 
    //    public void Put(int id, [FromBody]string value)
    //    {
    //    }

    //    // DELETE api/values/5 
    //    public void Delete(int id)
    //    {
    //    }
    //}

    //public class LaukikController : ApiController
    //{
    //    // GET api/values 
    //    public IEnumerable<string> Get()
    //    {
    //        return new string[] { "value1", "value2" };
    //    }

    //    // GET api/values/5 
    //    public string Get(int id)
    //    {
    //        return "Lauki";
    //    }

    //    // POST api/values 
    //    public IHttpActionResult Post([FromBody]string value)
    //    {
    //        return Ok<string>("Laukik APIs");
    //    }

    //    // PUT api/values/5 
    //    public void Put(int id, [FromBody]string value)
    //    {
    //    }

    //    // DELETE api/values/5 
    //    public void Delete(int id)
    //    {
    //    }
    //}

    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            //config.Services.Replace(typeof(IAssembliesResolver), new Program.MyNewAssembliesResolver());
            appBuilder.UseWebApi(config);
        }
    }

    //public class InternetDashboardController : ApiController
    //{
    //    public IHttpActionResult Post([FromBody]DweetSample value)
    //    {
    //        try
    //        {
    //            using (var client = new HttpClient())
    //            {
    //                StringContent content = new StringContent(value.Data, Encoding.UTF8, "application/json");
    //                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    //                var result = client.PostAsync("http://dweet.io/dweet/for/" + value.ResourceName, content).Result;
    //                if (result.IsSuccessStatusCode)
    //                    return Ok();
    //                return Content(result.StatusCode, result.ReasonPhrase);

    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return InternalServerError(ex);
    //        }
    //    }

    //}

    //public class DweetSample
    //{
    //    public string ResourceName { get; set; }
    //    public string Data { get; set; }
    //}
}
