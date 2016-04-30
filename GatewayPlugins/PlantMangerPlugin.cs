using Hub;
using Hub.Utilities;
using System;
using System.Collections.Generic;

namespace HubPlugins
{
    public class PlantMangerPlugin : IMultiSessionPlugin
    {
        private CacheService<float> _variableRam = null;
        private uint _defaultdrytime = 360;
        private byte _defaultWateringTime = 5;
        PlantDataSample _parsedData = null;

        public string Name
        {
            get
            {
                return "PM";
            }
        }

        public PlantMangerPlugin(uint dryTime, byte wateringTimeInSeconds, CacheService<float> cache)
        {
            if (dryTime == 0) throw new ArgumentNullException("dryTime cannot be zero");
            if (wateringTimeInSeconds == 0) throw new ArgumentNullException("wateringTimeInSeconds cannot be zero");
            _defaultdrytime = dryTime;
            _defaultWateringTime = wateringTimeInSeconds;
        }

        public void PostResponseProcess(IEnumerable<byte> requestData, IEnumerable<byte> responseData)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte> Respond(IEnumerable<byte> data)
        {
            //Parse Data
            //Run Algo
            //Answer to water or not in seconds to keep the pump on
            if (PlantDataSample.TryParse(data, out _parsedData))
            {
                return new byte[] { WateringAlgo(_parsedData) };
            }
            else
            {
                return new byte[0];
            }
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
                returnValue= _defaultWateringTime;
            }
            _variableRam.SetValue(dryCounterName, dryCounter);
            return returnValue;
        }

        public IMultiSessionPlugin Clone()
        {
            return new PlantMangerPlugin(_defaultdrytime, _defaultWateringTime, _variableRam);
        }
    }

    public class PlantDataSample
    {
        public float SoilMoisture { get; set; }
        public float SoilTemperature { get; set; }
        public bool IsWaterAvailable { get; set; }
        public DateTime TimestampLocal { get; set; }
        public string Id { get; set; }

        private PlantDataSample(Dictionary<string, string> parsedData)
        {
            SoilTemperature = float.Parse(parsedData["T"]);
            SoilMoisture = float.Parse(parsedData["M"]);
            IsWaterAvailable = bool.Parse(parsedData["W"]);
            TimestampLocal = DateTime.Now;
            Id = parsedData["I"];
        }

        public static bool TryParse(IEnumerable<byte> rawData, out PlantDataSample sample)
        {
            Dictionary<string, string> parsedData = null;
            var result = DataParser.TryParse(rawData, out parsedData);
            if (result == null)
            {
                sample = new PlantDataSample(parsedData);
                return true;
            }
            else
            {
                sample = null;
                Logger.Error(new Exception("PlantManager", result));
                return false;
            }
        }
    }
}
