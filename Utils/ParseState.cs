using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    /// <summary>
    /// ParseState Enum is used to define the states for the parsing the console arguments
    /// </summary>
       
    public enum ParseState
    {
        Unknown, Samples, Interval, Help, Predelay, RunAsChild
    }
}
