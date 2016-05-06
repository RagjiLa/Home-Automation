using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace Controllers
{
    public class InternetDashboardController : ApiController
    {
        public IHttpActionResult Post([FromBody]DweetSample value)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    StringContent content = new StringContent(value.Data, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var result = client.PostAsync("http://dweet.io/dweet/for/" + value.ResourceName, content).Result;
                    if (result.IsSuccessStatusCode)
                        return Ok();

                    return Content(result.StatusCode, result.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class DweetSample
        {
            public string ResourceName { get; set; }
            public string Data { get; set; }
        }
    }
}
