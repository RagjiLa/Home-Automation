using System;

namespace IOTGatewayHost.Business_Logic
{
    public static class Logger
    {
        public delegate void LogedEventHandler(object sender, LoggedArgs e);
        public static event LogedEventHandler Logged;

        public static void Log(string msg)
        {
            if (Logged != null) Logged(Environment.StackTrace, new LoggedArgs(LoggedType.Log, msg + Environment.NewLine));
        }

        public static void Warning(string msg)
        {
            if (Logged != null) Logged(Environment.StackTrace, new LoggedArgs(LoggedType.Warning, msg + Environment.NewLine));
        }

        public static void Error(Exception ex)
        {
            Error(ex.ToString());
        }

        public static void Error(string msg)
        {
            if (Logged != null) Logged(Environment.StackTrace, new LoggedArgs(LoggedType.Error, msg + Environment.NewLine));
        }
    }

    public enum LoggedType
    {
        Log,
        Warning,
        Error
    }

    public class LoggedArgs : EventArgs
    {
        private readonly LoggedType _type;
        private readonly string _msg;
        public LoggedArgs(LoggedType type, string msg)
        {
            _type = type;
            _msg = msg;
        }
        public LoggedType Type { get { return _type; } }
        public string Message { get { return _msg; } }
    }
}
