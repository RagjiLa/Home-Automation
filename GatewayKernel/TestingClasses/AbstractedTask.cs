using Hub.TestingInterfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hub.TestingClasses
{
    public class AbstractedTask : ITask
    {
        public void Run(Action block, string name)
        {
            Task.Run(() =>
            {
                Thread.CurrentThread.Name = name;
                block();
            });
        }
    }
}
