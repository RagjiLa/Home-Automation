using Hub;
using Kernel;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System;

namespace HubPlugins
{
    public class DweetPlugin : IMultiSessionPlugin
    {
        public ISample AssociatedSample
        {
            get
            {
                return new DweetSample();
            }
        }

        public PluginName Name
        {
            get
            {
                return PluginName.DweetPlugin;
            }
        }

        public IMultiSessionPlugin Clone()
        {
            return new DweetPlugin();
        }

        public void Invoke(ISample requestSample, Action<IEnumerable<byte>> sendResponse, MessageBus interPluginCommunicationBus)
        {
            sendResponse(new byte[0]);
            using (var client = new HttpClient())
            {
                var sample = requestSample as DweetSample;
                // ReSharper disable once PossibleNullReferenceException
                StringContent content = new StringContent(sample.Data, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var result = client.PostAsync("http://dweet.io/dweet/for/" + sample.Tag, content).Result;
            }
        }

        public void ShutDown()
        {

        }
    }

    public class DweetSample : ISample
    {
        public string Tag { get; private set; }
        public string Data { get; private set; }

        public DweetSample(string tag, string jsonData)
        {
            Tag = tag;
            Data = jsonData;
        }
        public DweetSample()
        {

        }

        public IDictionary<string, string> ToKeyValuePair()
        {
            var kvpData = new Dictionary<string, string>();
            kvpData.Add("T", Tag);
            kvpData.Add("D", Data);
            return kvpData;
        }

        public void FromKeyValuePair(IDictionary<string, string> kvpData)
        {
            Tag = kvpData["T"];
            Data = kvpData["D"];
        }
    }
}
