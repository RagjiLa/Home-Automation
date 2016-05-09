using ControllerProxies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace DeviceControllers
{
    public class TemperatureController : ApiController
    {
        [HttpPost]
        [Route("Temperature/{uniqueName}")]
        public IHttpActionResult Post(string uniqueName, [FromBody]TemperatureData data)
        {
            try
            {
                if (data == null) return BadRequest("Resource data cannot be empty null or undifiend");
                if (string.IsNullOrWhiteSpace(uniqueName) || string.IsNullOrEmpty(uniqueName)) return BadRequest("Resource name is required.");
                using (var timeSeriesProxy = (TimeSeriesProxy)Configuration.DependencyResolver.GetService(typeof(TimeSeriesProxy)))
                {
                    timeSeriesProxy.SetData(uniqueName + "_RSSI", DateTime.Now, data.RSSI.ToString(CultureInfo.InvariantCulture));
                    timeSeriesProxy.SetData(uniqueName + "_WakeTime", DateTime.Now, data.WakeTime.ToString(CultureInfo.InvariantCulture));
                    timeSeriesProxy.SetData(uniqueName + "_HeatIndex", DateTime.Now, data.HeatIndex.ToString(CultureInfo.InvariantCulture));
                    timeSeriesProxy.SetData(uniqueName + "_Humidity", DateTime.Now, data.Humidity.ToString(CultureInfo.InvariantCulture));
                    timeSeriesProxy.SetData(uniqueName + "_Temperature", DateTime.Now, data.Temperature.ToString(CultureInfo.InvariantCulture));
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class TemperatureData
        {
            public float Temperature { get; set; }
            public float Humidity { get; set; }
            public float HeatIndex { get; set; }
            public float RSSI { get; set; }
            public long WakeTime { get; set; }
        }
    }
}
