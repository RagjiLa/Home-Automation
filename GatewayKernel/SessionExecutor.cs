using System;
using System.Collections.Generic;
using System.Threading;

namespace Hub
{
    public class SessionExecutor : ISingleSessionPlugin, IDisposable
    {
        private ISingleSessionPlugin _plugin;
        private object _singleThreadSync = new object();
        private bool _disposed = false;
        Mutex _sessionSync = new Mutex();

        public string Name
        {
            get
            {
                return _plugin.Name;
            }
        }

        public bool CanHaveMultipleSessions
        {
            get
            {
                return _plugin is IMultiSessionPlugin;
            }
        }

        public SessionExecutor(ISingleSessionPlugin plugin)
        {
            _plugin = plugin;
        }

        public void PostResponseProcess(IEnumerable<byte> requestData, IEnumerable<byte> responseData)
        {
            try
            {
            Wait: if (_sessionSync.WaitOne(3))
                {
                    if (!_disposed) _plugin.PostResponseProcess(requestData, responseData);
                }
                else
                {
                    if (!_disposed) goto Wait;
                }
            }
            finally
            {
                if (!_disposed) _sessionSync.ReleaseMutex();
            }
        }

        public IEnumerable<byte> Respond(IEnumerable<byte> data)
        {

        Wait: if (_sessionSync.WaitOne(3))
            {
                if (!_disposed) return _plugin.Respond(data);
            }
            else
            {
                if (!_disposed) goto Wait;
            }
            return null;
        }

        public SessionExecutor CreateNewSession()
        {
            if (!CanHaveMultipleSessions) throw new InvalidOperationException("Plugin doesnot support multiple sessions.");
            lock (_singleThreadSync)
            {
                return new SessionExecutor((_plugin as IMultiSessionPlugin).Clone());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourceCleanUp)
        {
            if (managedResourceCleanUp)
            {
                // free managed resources
                _disposed = true;
                _plugin = null;
                _sessionSync.Dispose();
            }

            // free native resources if there are any.
        }
    }
}
