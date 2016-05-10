using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ControllerProxies
{
    public class TimeSeriesProxy : HttpClient
    {
        public TimeSeriesProxy(string baseUrl)
        {
            base.BaseAddress = new Uri(baseUrl);
        }

        public void SetData(string seriesName, DateTime localTimestamp, string value)
        {
            StringContent dummyConten = new StringContent("", Encoding.UTF8, "application/json");
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = PostAsync("TimeSeries/" + seriesName + "/" + localTimestamp.ToUniversalTime().ToString("o") + "/" + value, dummyConten).Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Failed");
        }
    }
}
