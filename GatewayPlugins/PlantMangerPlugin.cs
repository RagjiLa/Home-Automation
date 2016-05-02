using Hub;
using Kernel;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HubPlugins
{
    public class PlantMangerPlugin : IMultiSessionPlugin
    {
        private readonly CacheService<float> _variableRam ;
        private readonly uint _defaultdrytime;
        private readonly byte _defaultWateringTime;
        private PlantDataSample _parsedData;

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

        public PlantMangerPlugin(uint dryTime, byte wateringTimeInSeconds, CacheService<float> cacheService)
        {
            if (dryTime == 0) throw new ArgumentNullException("dryTime");
            if (wateringTimeInSeconds == 0) throw new ArgumentNullException("wateringTimeInSeconds");
            _defaultdrytime = dryTime;
            _defaultWateringTime = wateringTimeInSeconds;
            _variableRam = cacheService;
        }

        public IEnumerable<byte> Respond(ISample sample)
        {
            _parsedData = sample as PlantDataSample;
            return new [] { WateringAlgo(_parsedData) };
        }

        private byte WateringAlgo(PlantDataSample parsedData)
        {
            byte returnValue = 0;
            string dryCounterName = parsedData.Id + "dryCounter";
            var dryCounter = _variableRam.GetValue(dryCounterName);
            dryCounter--;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (dryCounter == 0.0f)
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
        public DateTime TimestampUtc { get; private set; }
        public string Id { get; private set; }

        public static bool TryParse(IEnumerable<byte> rawData, out PlantDataSample sample)
        {
            try
            {
                var parsedData = DataParser.ToKeyValuePairs(rawData);

                sample = new PlantDataSample();
                sample.FromKeyValuePair(parsedData);
                return true;
            }
            catch (Exception ex)
            {
                sample = null;
                Logger.Error(new Exception("PlantDataSample", ex));
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
            kvpData.Add("Ts", TimestampUtc.ToBinary().ToString(CultureInfo.InvariantCulture));
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
                TimestampUtc = DateTime.FromBinary(longDt);
            }
            else
            {
                TimestampUtc = DateTime.UtcNow;
            }
        }
    }
}
