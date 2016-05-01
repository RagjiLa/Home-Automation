﻿using Hub;
using Hub.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HubPlugins
{
    public class DweetPlugin : IMultiSessionPlugin
    {
        public ISample AssociatedSample
        {
            get
            {
                return new DweetSample(); ;
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

        public void PostResponseProcess(ISample requestSample, IEnumerable<byte> responseData, MessageBus communicationBus)
        {
            using (var client = new HttpClient())
            {
                var sample = requestSample as DweetSample;
                StringContent content = new StringContent(sample.Data, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PostAsync("http://dweet.io/dweet/for/" + sample.Tag, content).Result;
            }
        }

        public IEnumerable<byte> Respond(ISample sample)
        {
            return new byte[0];
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
