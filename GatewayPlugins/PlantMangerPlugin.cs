using Hub;
using Hub.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace HubPlugins
{
    public class PlantMangerPlugin : IMultiSessionPlugin
    {
        private CacheService<float> _variableRam = null;
        private uint _defaultdrytime = 360;
        private byte _defaultWateringTime = 5;
        private PlantDataSample _parsedData = null;

        public ISample AssociatedSample
        {
            get
            {
                return new PlantDataSample(); 
            }
        }

        public PluginName Name
        {
            get
            {
                return PluginName.PlantManagerPlugin;
            }
        }

        public PlantMangerPlugin(uint dryTime, byte wateringTimeInSeconds, CacheService<float> cache)
        {
            if (dryTime == 0) throw new ArgumentNullException("dryTime cannot be zero");
            if (wateringTimeInSeconds == 0) throw new ArgumentNullException("wateringTimeInSeconds cannot be zero");
            _defaultdrytime = dryTime;
            _defaultWateringTime = wateringTimeInSeconds;
        }

        public IEnumerable<byte> Respond(ISample sample)
        {
            //Run Algo
            //Answer to water or not in seconds to keep the pump on

            _parsedData = sample as PlantDataSample;
            return new byte[] { WateringAlgo(_parsedData) };
        }

        private byte WateringAlgo(PlantDataSample parsedData)
        {
            byte returnValue = 0;
            string dryCounterName = parsedData.Id + "dryCounter";
            var dryCounter = _variableRam.GetValue(dryCounterName);
            dryCounter--;
            if (dryCounter == 0)
            {
                dryCounter = _defaultdrytime;
                returnValue = _defaultWateringTime;
            }
            _variableRam.SetValue(dryCounterName, dryCounter);
            return returnValue;
        }

        public IMultiSessionPlugin Clone()
        {
            return new PlantMangerPlugin(_defaultdrytime, _defaultWateringTime, _variableRam);
        }

        public void PostResponseProcess(ISample requestSample, IEnumerable<byte> responseData, MessageBus communicationBus)
        {
            var sample = new DweetSample(_parsedData.Id, _parsedData.ToJsonString());
            communicationBus.Invoke(PluginName.DweetPlugin, sample);
        }
    }

    public class PlantDataSample : ISample
    {
        public float SoilMoisture { get; private set; }
        public float SoilTemperature { get; private set; }
        public bool IsWaterAvailable { get; private set; }
        public DateTime TimestampUTC { get; private set; }
        public string Id { get; private set; }

        public static bool TryParse(IEnumerable<byte> rawData, out PlantDataSample sample)
        {
            IDictionary<string, string> parsedData = null;
            var result = DataParser.TryParse(rawData, out parsedData);
            if (result == null)
            {
                sample = new PlantDataSample();
                sample.FromKeyValuePair(parsedData);
                return true;
            }
            else
            {
                sample = null;
                Logger.Error(new Exception("PlantDataSample", result));
                return false;
            }
        }

        public IDictionary<string, string> ToKeyValuePair()
        {
            var kvpData = new Dictionary<string, string>();
            kvpData.Add("T", SoilTemperature.ToString(CultureInfo.InvariantCulture));
            kvpData.Add("M", SoilMoisture.ToString(CultureInfo.InvariantCulture));
            kvpData.Add("W", IsWaterAvailable.ToString(CultureInfo.InvariantCulture));
            kvpData.Add("I", Id);
            kvpData.Add("Ts", TimestampUTC.ToBinary().ToString(CultureInfo.InvariantCulture));
            return kvpData;
        }

        public void FromKeyValuePair(IDictionary<string, string> kvpData)
        {
            Id = kvpData["I"];
            SoilTemperature = float.Parse(kvpData["T"], CultureInfo.InvariantCulture);
            SoilMoisture = float.Parse(kvpData["M"], CultureInfo.InvariantCulture);
            IsWaterAvailable = bool.Parse(kvpData["W"]);
            if (kvpData.ContainsKey("Ts"))
            {
                var longDt = long.Parse(kvpData["Ts"], CultureInfo.InvariantCulture);
                TimestampUTC = DateTime.FromBinary(longDt);
            }
            else
            {
                TimestampUTC = DateTime.UtcNow;
            }
        }
    }
}
