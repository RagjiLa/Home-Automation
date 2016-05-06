using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DeviceControllers
{
    public class PlantController : ApiController
    {
        public IHttpActionResult Post([FromBody]Dictionary<string, string> timeseriesData)
        {
            return Ok();
        }

        public class RequestData
        {
            public float SoilTemperature { get; set; }
            public float SoilHumidity { get; set; }
            public int WaterLevel { get; set; }
            public float RSSI { get; set; }
            public long WakeTime { get; set; }
            public string ID { get; set; }
        }

        public class ResponseData
        {
            public int PumpOnInSeconds { get; set; }
        }
    }
}
