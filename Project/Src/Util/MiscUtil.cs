using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.TextEditor.Src.Util
{
    /// <summary>Device wants to say something.</summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>How bad is it doctor?</summary>
        public string Level { get; set; } = "???";

        /// <summary>Content.</summary>
        public string Message { get; set; } = null;

        /// <summary>From log4net.</summary>
        public DateTime TimeStamp { get; set; }
    }

    /// <summary>
    /// Poor man's logger for now. Maybe use NLog later. TODO2.
    /// </summary>
    public class Logger
    {
        /// <summary>Request for logging service.</summary>
        public static event EventHandler<LogEventArgs> LogEvent;

        public static void Error(params object[] vars)
        {
            Write($"ERR ", vars);
        }

        public static void Warn(params object[] vars)
        {
            Write($"WRN ", vars);
        }

        public static void Info(params object[] vars)
        {
            Write($"INF ", vars);
        }

        static void Write(string cat, params object[] vars)
        {
            LogEvent?.Invoke(null, new LogEventArgs() { Level = cat, TimeStamp = DateTime.Now, Message = string.Join(" ", vars) });
        }
    }
}
