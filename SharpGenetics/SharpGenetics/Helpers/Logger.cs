using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Logging
{
    public static class Logger
    {
        static StreamWriter File = new StreamWriter("log.txt");
        public static void Log(string LogMessage)
        {
            lock (File)
            {
                File.WriteLine(LogMessage);
                File.Flush();
            }
        }

        public static void Log(double LogNumber)
        {
            Log(LogNumber.ToString());
        }
    }
}
