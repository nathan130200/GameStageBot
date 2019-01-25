using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GameStage
{
    public static class Log
    {
        static Log()
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        }

        static string Prepare(string message)
        {
            return $"[{DateTime.Now.ToString("dd-MM-yyyy'T'HH:mm:ss.fff")}] " +
                $"[B#{typeof(Log).Assembly.GetName().Version.Build}] " +
                Regex.Replace(message.ToString(), "(\r\n|\r|\n)", Environment.NewLine);
        }

        public static void Info(string message)
            => Trace.TraceInformation(Prepare(message));

        public static void Info(string format, params object[] args)
            => Info(string.Format(format, args));

        public static void Warn(string message)
            => Trace.TraceWarning(Prepare(message));

        public static void Warn(string format, params object[] args)
            => Warn(string.Format(format, args));

        public static void Error(string message)
            => Trace.TraceError(Prepare(message));

        public static void Error(string format, params object[] args)
            => Error(string.Format(format, args));
    }
}
