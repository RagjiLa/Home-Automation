using Kernel;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Hub
{
    public class SessionExecutor : ISingleSessionPlugin, IDisposable
    {
        private ISingleSessionPlugin _plugin;
        private readonly object _singleThreadSync = new object();
        private bool _disposed;
        private readonly AutoResetEvent _sessionSync = new AutoResetEvent(true);

        public PluginName Name
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

        public ISample AssociatedSample
        {
            get
            {
                return _plugin.AssociatedSample;
            }
        }

        public SessionExecutor(ISingleSessionPlugin plugin)
        {
            _plugin = plugin;
        }

        public void Invoke(ISample sample, Action<IEnumerable<byte>> sendResponse, MessageBus communicationBus)
        {
            try
            {
            Wait: if (_sessionSync.WaitOne(3))
                {
                    if (!_disposed) _plugin.Invoke(sample, sendResponse, communicationBus);
                }
                else
                {
                    if (!_disposed)
                        goto Wait;
                }
            }
            finally
            {
                if (!_disposed)
                    _sessionSync.Set();
            }
        }

        public SessionExecutor CreateNewSession()
        {
            lock (_singleThreadSync)
            {
                if (!CanHaveMultipleSessions) throw new InvalidOperationException("Plugin doesnot support multiple sessions.");
                // ReSharper disable once PossibleNullReferenceException
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

        public void ShutDown()
        {
            _plugin.ShutDown();
        }
    }
}
