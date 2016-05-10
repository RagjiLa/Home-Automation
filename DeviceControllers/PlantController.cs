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
        [HttpPost]
        [Route("Plant")]
        public IHttpActionResult Post([FromBody]RequestData data)
        {
            //Historise data
            //Calculate water needs to be put
            //raise any alarms if necessary
            //dash board display if any display
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
