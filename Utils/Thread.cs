using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StackTracer.Utils
{
    /// <summary>
    /// Thread object contains the information related to the current thread for which stack
    /// trace is being generated
    /// </summary>
    [XmlRoot(ElementName = "Thread")]
    public class STThread
    {
        public STThread()
        {
        }
        public STThread(DateTime stackCaptureTime, List<StackFrame> stackTrace)
        {
            this.sampleCaptureTime = stackCaptureTime;
            this.stackTrace = stackTrace;
        }
        public DateTime sampleCaptureTime { get; set; }
        public int managedThreadId { get; set; }
        public uint oSID { get; set; }
        public List<StackFrame> stackTrace { get; set; }
        public DateTime? RequestTimeStamp { get; set; }
        public bool HasHttpContext { get; set; }

        public string RequestUrl { get; set; }

        public ulong HttpContextAddress { get; set; }

        public ulong WorkerRequestAddress { get; set; }

        public ulong HttpRequestAddress { get; set; }

        public ulong HttpResponseAddress { get; set; }
        public bool IsRequestCompleted { get; set; }


        public bool IsRequestEnded { get; set; }

        public bool IsFlushing { get; set; }

    }
}
