using Hub;
using System;
using System.Collections.Generic;

namespace HubPlugins
{
    public class PlantMangerPlugin : ISingleSessionPlugin
    {
        public string Name
        {
            get
            {
                return "PlantManager";
            }
        }

        public void PostResponseProcess(IEnumerable<byte> requestData, IEnumerable<byte> responseData)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte> Respond(IEnumerable<byte> data)
        {
            throw new NotImplementedException();
        }
    }
}
