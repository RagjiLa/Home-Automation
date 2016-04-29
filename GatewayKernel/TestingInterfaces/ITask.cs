using System;

namespace GatewayKernel.TestingInterfaces
{
    public interface ITask
    {
        void Run(Action block,string blockName);
    }
}
