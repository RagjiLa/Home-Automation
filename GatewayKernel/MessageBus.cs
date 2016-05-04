using Kernel;
using System;
using System.Collections.Generic;

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

        public void Invoke(PluginName name, Action<IEnumerable<byte>> responseAction, ISample sample)
        {
            lock (_owner)
            {
                if (name.ToString() == _owner.ToString())
                    throw new InvalidOperationException("Plugins cannot call to self");

                if (_responders.ContainsKey(name.ToString()))
                {
                    var handler = _responders[name.ToString()];
                    handler.Invoke(sample, responseAction, this);
                }
                else
                {
                    Logger.Error("Message Bus No handlers for " + name);
                }
            }
        }
    }
}
