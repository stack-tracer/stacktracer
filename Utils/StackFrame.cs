using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    /// <summary>
    /// StackFrame is the object which is being used to hold the data for a single stack trace for a 
    /// particular thread.
    /// </summary>
    public class StackFrame
    {
        public StackFrame()
        {

        }
        public StackFrame(string stackTraceString, ulong instructionPointer, string clrMethodString, ulong StackPointer)
        {
            this.stackTraceString = stackTraceString;
            this.instructionPointer = instructionPointer;
            this.clrMethodString = clrMethodString;
            this.stackPointer = StackPointer;
        }
        public string stackTraceString { get; set; }
        public ulong instructionPointer { get; set; }
        // get this from clrmethod - GetFullSignature()
        public string clrMethodString { get; set; }
        public ulong stackPointer { get; set; }
    }
}
