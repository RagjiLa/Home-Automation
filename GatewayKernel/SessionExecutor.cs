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
        readonly Mutex _sessionSync = new Mutex();

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

        public void PostResponseProcess(ISample requestSample, IEnumerable<byte> responseData, MessageBus communicationBus)
        {
            try
            {
            Wait: if (_sessionSync.WaitOne(3))
                {
                    if (!_disposed) _plugin.PostResponseProcess(requestSample, responseData, communicationBus);
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

        public IEnumerable<byte> Respond(ISample sample)
        {

        Wait: if (_sessionSync.WaitOne(3))
            {
                if (!_disposed) return _plugin.Respond(sample);
            }
            else
            {
                if (!_disposed) goto Wait;
            }
            return null;
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
    }
}
