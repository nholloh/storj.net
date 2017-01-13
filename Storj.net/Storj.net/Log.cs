using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Storj.net
{
    class Log
    {
        static StreamWriter _log;
        static DateTime _start = default(DateTime);
        static string _progressDirectoryName;
        static string _titleHr;
        static string _appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "storj");
        public static bool DebugMode { get; set; }

        static Log()
        {
            if (!Directory.Exists(_appData))
                Directory.CreateDirectory(_appData);
            try
            {
                _log = new StreamWriter(new FileStream(Path.Combine(_appData, "storj.net.log.txt"), System.IO.FileMode.Create));
            }
            catch (Exception e) { }  
        }

        [DebuggerStepThrough]
        public static void Clear()
        {
            Console.Clear();
        }

        [DebuggerStepThrough]
        public static void Text(string Msg, params object[] Args)
        {
            Write("INFO", ProcessString(Msg, Args));
        }

        [DebuggerStepThrough]
        public static void Warning(string Msg, params object[] Args)
        {
            Write("WARN", ProcessString(Msg, Args));
        }

        //[DebuggerStepThrough]
        public static void Debug(string Msg, params object[] Args)
        {
            if (!DebugMode)
                return;
            Write("DEBU", ProcessString(Msg, Args));
        }

        [DebuggerStepThrough]
        public static void Error(string Msg, params object[] Args)
        {
            Write("ERRO", ProcessString(Msg, Args));
        }

        [DebuggerStepThrough]
        public static void Error(string Msg, Exception ex, params object[] Args)
        {
            Write("ERRO", ProcessString(Msg + "\n Exception: " + ex.ToString(), Args));
        }

        [DebuggerStepThrough]
        public static short ReadShort()
        {
            while (true)
            {
                try
                {
                    return Int16.Parse(Console.ReadLine());
                }
                catch (Exception e) { }
            }
        }

        [DebuggerStepThrough]
        private static string ProcessString(string Msg, object[] Args)
        {
            for (int i = 0; i < Args.Length; i++)
                Msg = Msg.Replace("{" + i.ToString() + "}", Args[i].ToString());
            return Msg;
        }

        [DebuggerStepThrough]
        private static void Write(string Type, string Msg)
        {
            Write(Type, Msg, true);
        }

        [DebuggerStepThrough]
        private static void Write(string Type, string Msg, bool WriteLog)
        {
            string _logMsg = DateTime.Now.ToString("dd.MM.yy-hh:mm:ss");
            _logMsg += " - [" + Type + "]: ";
            _logMsg += Msg;
            Console.WriteLine(_logMsg);

            if (WriteLog && _log != null)
            {
                _log.WriteLine(_logMsg);
                _log.Flush();
            }
        }

        internal static void CloseLog()
        {
            _log.Close();
        }
    }
}
