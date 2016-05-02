using System.Collections.Concurrent;

namespace Hub.Utilities
{
    public class FiniteQue<T>
    {
        ConcurrentQueue<T> q = new ConcurrentQueue<T>();

        public int Limit { get; set; }

        public void Enqueue(T obj)
        {
            lock (this)
            {
                q.Enqueue(obj);
                T overflow;
                while (q.Count > Limit && q.TryDequeue(out overflow))
                {
                    //Keep on doing deque.
                }
            }
        }
    }
}
