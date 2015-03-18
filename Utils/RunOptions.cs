using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    public class RunOptions
    {
        //ref state, ref runAsRemote, ref processName, ref pId, ref delay, ref sampleCount, ref pdelay, ref arguments, ref returnFromConsole
        public bool RunAsRemote { get; set; }
        public string ProcessName { get; set; }
        public int PID { get; set; }
        public int InitialDelay { get; set; }

        public int SamplesCount { get; set; }
        public int Interval { get; set; }

        public ParseState State { get; set; }

    }
}
