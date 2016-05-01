using Hub.Utilities;
using System.Collections.Generic;

namespace Hub
{
    public interface ISingleSessionPlugin
    {
        PluginName Name { get; }
        ISample AssociatedSample { get; }
        IEnumerable<byte> Respond(ISample sample);
        void PostResponseProcess(ISample requestSample, IEnumerable<byte> responseData, MessageBus communicationBus);
    }

}
