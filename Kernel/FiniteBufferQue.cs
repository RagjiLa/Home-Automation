using System.Collections.Concurrent;

namespace Kernel
{
    public class FiniteBufferQue<T>
    {
        readonly ConcurrentQueue<T> _bufferq = new ConcurrentQueue<T>();

        public int Limit { get; set; }

        public FiniteBufferQue(int limit)
        {
            Limit = limit;
        }

        public bool Enqueue(T obj)
        {
            lock (this)
            {
                _bufferq.Enqueue(obj);
                return _bufferq.Count > Limit;
            }
        }

        public bool TryDequeue(out T result)
        {
            lock (this)
            {
                return _bufferq.TryDequeue(out result);
            }
        }
    }
}
