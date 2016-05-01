using Hub.Utilities;
using System;
using System.Collections.Generic;

namespace Hub
{
    public class MessageBus
    {
        private Dictionary<string, SessionExecutor> _responders = new Dictionary<string, SessionExecutor>();
        private PluginName _owner;
        public MessageBus(Dictionary<string, SessionExecutor> responders, PluginName owner)
        {
            _responders = responders;
            _owner = owner;
        }
        public IEnumerable<byte> Invoke(PluginName name, ISample sample)
        {
            if (name.ToString() == _owner.ToString())
                throw new InvalidOperationException("Plugins cannot call to self");

            if (_responders.ContainsKey(name.ToString()))
            {
                var handler = _responders[name.ToString()];
                var response = handler.Respond(sample);
                handler.PostResponseProcess(sample, response, this);
                return response;
            }
            else
            {
                Logger.Error("Message Bus No handlers for " + name.ToString());
                return null;
            }
        }
    }
}
