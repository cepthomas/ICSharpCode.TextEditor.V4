using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.TextEditor.Src.Util
{
    /// <summary>
    /// Poor man's logger for now. Maybe use NLog later.
    /// </summary>
    public class Logger
    {
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
            string time = DateTime.Now.ToString("yyyy'-'MM'-'dd HH':'mm':'ss.fff");
            Console.WriteLine($"{time} {cat} {string.Join(" ", vars)}");
        }
    }
}
