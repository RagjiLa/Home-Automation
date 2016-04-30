using System.Collections.Generic;

namespace Hub
{
    public interface ISingleSessionPlugin
    {
        string Name { get; }
        IEnumerable<byte> Respond(IEnumerable<byte> data);
        void PostResponseProcess(IEnumerable<byte> requestData, IEnumerable<byte> responseData);
    }
   
}
