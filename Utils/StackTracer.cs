using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    /// <summary>
    /// StackTracer class is used to contain the stacktracer object which 
    /// is seralized to xml object after collecting stack traces.
    /// </summary>
    public class StackTracer
    {
        public StackTracer()
        {

        }
        public string processName { get; set; }
        public int processID { get; set; }
        public List<StackSample> sampleCollection { get; set; }
    }
}
