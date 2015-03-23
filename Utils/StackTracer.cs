using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StackTracer.Utils
{
    /// <summary>
    /// StackTracer class is used to contain the stacktracer object which 
    /// is seralized to xml object after collecting stack traces.
    /// </summary>
    public class StackCaptures
    {
        public StackCaptures(string processName,int processId)
        {
            ProcessName = processName;
            ProcessID = processId;
            Samples = new List<StackSample>();
        }
        
        [XmlElement(ElementName = "processName")]
        public string ProcessName { get; set; }

        [XmlElement(ElementName = "processID")]
        public int ProcessID { get; set; }
        public List<StackSample> Samples { get; set; }
        public void AddSample(StackSample sample)
        { 
            Samples.Add(sample);
        }
        public Summary Summary { get { return CalculateSummary(); } }

        private Summary CalculateSummary()
        {
            throw new NotImplementedException();
        }
    }
    public class Summary
    {
        public String RequestUrl { get; set; }
        public DateTime StartTime { get; set; }
        public Boolean IsFinished { get; set; }

        public String CLRMethod { get; set; }

    }
}
