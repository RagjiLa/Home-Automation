using Kernel;
using System;
using System.Collections.Generic;

namespace Hub
{
    public interface ISingleSessionPlugin
    {
        PluginName Name { get; }
        ISample AssociatedSample { get; }
        void Invoke(ISample sample,Action<IEnumerable<byte>> sendResponse,MessageBus interPluginCommunicationBus);
        void ShutDown();
    }

}
