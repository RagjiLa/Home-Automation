﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;

namespace Controllers
{
    public class InternetDashboardController : ApiController
    {
        [HttpPost]
        [Route("InternetDashboard/{uniqueName}/{jsonValue}")]
        public IHttpActionResult Post(string uniqueName,string jsonValue)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    StringContent content = new StringContent(jsonValue, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var result = client.PostAsync("http://dweet.io/dweet/for/" + uniqueName, content).Result;
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
    }
}
