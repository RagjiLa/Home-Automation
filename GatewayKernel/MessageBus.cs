using Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hub
{
    public class MessageBus
    {
        private readonly Dictionary<string, SessionExecutor> _responders;
        private readonly PluginName _owner;

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
                var response = handler.Respond(sample).ToArray();
                handler.PostResponseProcess(sample, response, this);
                return response;
            }
            else
            {
                Logger.Error("Message Bus No handlers for " + name);
                return null;
            }
        }
    }
}
