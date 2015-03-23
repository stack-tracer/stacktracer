using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    /// <summary>
    /// StackSample is the datastructure to hold the data for single SatckSample
    /// </summary>
    public class StackSample
    {
        public StackSample()
        {

        }
        public int sampleCounter { get; set; }
        public DateTime samplingTime { get; set; }
        public int threadCount { get; set; }
        public List<STThread> processThreadCollection { get; set; }
    }
}
