using System;

namespace Hub.TestingInterfaces
{
    public interface ITask
    {
        void Run(Action block,string blockName);
    }
}
