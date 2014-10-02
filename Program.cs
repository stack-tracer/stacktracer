using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Security.Cryptography;
using System.Security;
using System.Runtime.InteropServices;

namespace StackTracer
{ 
    
    class Program
    {
        private static bool FileHashChecked = false;
        /// <summary>
        /// ParseState Enum is used to define the states for the parsing the console arguments
        /// </summary>
        enum ParseState
        {
            Unknown, Samples, Interval,Help,Predelay
        }
        enum Bitness
        {
            UnKnown,x86,x64
        }

       static void Main(string[] args)
        {
            
            StringBuilder StackTracerLogger = new StringBuilder();
            var state = ParseState.Unknown;
            Bitness currentProcessBitness, targetProcessBitness;
           try
            {
                // Global variable declaration

                int pid = -1;
                string processName ="";
                int Pid = -1;
                int delay = 500;
                int stackTraceCount = 10;
                string stacktraceLocation = null;
                int pdelay = 0;
                
               // Getting the parameters inatilized 
                #region Region for setting the console parameters switches   

               //if no arguments are paased ,show help menu
                if (args.ToList<string>().Count != 0)
                {
                    foreach (var arg in args.Skip(1))
                    {
                        switch (state)
                        {
                            case ParseState.Unknown:
                                if (arg.ToLower() == "/s")
                                {
                                    state = ParseState.Samples;
                                }
                                else if (arg.ToLower() == "/i")
                                {
                                    state = ParseState.Interval;
                                }
                                else if (arg.ToLower() == "/d")
                                {
                                    state = ParseState.Predelay;
                                }
                                else
                                {
                                    Usage();
                                    state = ParseState.Help;
                                    return;
                                }
                                break;
                            case ParseState.Samples:
                                if (!int.TryParse(arg, out stackTraceCount))
                                {
                                    Usage();
                                    state = ParseState.Help;
                                    return;
                                }
                                state = ParseState.Unknown;
                                break;
                            case ParseState.Interval:
                                if (!int.TryParse(arg, out delay))
                                {
                                    Usage();
                                    state = ParseState.Help;
                                    return;
                                }
                                state = ParseState.Unknown;
                                break;
                            case ParseState.Predelay:
                                if (!int.TryParse(arg, out pdelay))
                                {
                                    Usage();
                                    state = ParseState.Help;
                                    return;
                                }
                                state = ParseState.Unknown;
                                break;
                            default:
                                state = ParseState.Help;
                                break;
                        }
                    }

                    //if the first argument is pid,parse it ,otherwise take it as process name.
                    if (!int.TryParse(args[0], out Pid))
                    {

                        if (args[0] != null && args[0].Length != 0)
                        {
                            if (args[0].ToLower() == "/?")
                            {
                                state = ParseState.Help;
                                Usage();
                            }
                            else
                            {
                                processName = args[0];
                            }
                            //else
                            //{
                            //    processName = "w3wp";
                            //    StackTracerLogger.AppendLine("The switch for process is not provided using [w3wp] as default process name");                    
                            //}
                        }
                    }
                }
                else
                {
                    Usage();
                    state = ParseState.Help;


                }
                #endregion

                Process targetProcess = null;
               if (state != ParseState.Help)
               {
                  if (string.IsNullOrEmpty(processName))
                       pid = Pid;
                   try
                   {
                       //check for multiple process with same names
                       Process[] processes = Process.GetProcessesByName(processName);
                       if (processes.Length>1)
                       {
                           string msg = string.Format("There are multiple processes with name {0} currently running,please pass PID", processName);
                           Console.WriteLine(msg);
                           
                       }
                       else if (processes.Length == 1)
                       {
                           targetProcess = processes[0];
                           //get the process with process names
                           pid = targetProcess.Id;
                       }
                   }
                   catch (Exception ex)
                   {
                       if (processName != "")
                           Console.WriteLine("Process with name {0} Doesn't Exist", processName);
                       else
                       {
                           try
                           {
                              targetProcess=Process.GetProcessById(pid);

                           }
                           catch
                           {
                               Console.WriteLine("Process with PID {0} Doesn't Exist", pid);
                           }
                       }
                       StackTracerLogger.AppendLine("Process with name"+ processName + " doesn't exist");
                       throw ex;
                      
                   }
                   if (targetProcess!=null)
                   {
                       if (!IsWow64(Process.GetCurrentProcess()))
                       {
                           currentProcessBitness=Bitness.x64;
                       }
                       else
                           currentProcessBitness=Bitness.x86;

                       if (!IsWow64(targetProcess))
                       {
                           targetProcessBitness = Bitness.x64;
                       }
                       else
                       {
                           targetProcessBitness = Bitness.x86;
                       }

                       if (currentProcessBitness!=targetProcessBitness)
                       {

                           string errorMessage = string.Format("Process bitness mismatch!, Stacktracer.exe is of {0} and Target process {2} is of {1}. Please use {3} StackTracer.exe", currentProcessBitness == Bitness.x64 ? "64 bit" : "32 bit",
                               targetProcessBitness == Bitness.x64 ? "64 bit" : "32 bit", targetProcess.ProcessName, currentProcessBitness == Bitness.x64 ? "32 bit" : "64 bit");
                           Console.WriteLine(errorMessage);
                           throw new Exception(errorMessage);
                       }
                       if (pdelay>0)
                       {
                           Console.WriteLine("Initiating the stacktrace capture after given delay of {0} seconds....", pdelay);
                       }
                       
                       
                       System.Threading.Thread.Sleep(pdelay * 1000);
 
                       //StackTracerLogger.AppendLine("The selected pid for the process " + processName + " is " + pid);
                       StackTracer stackTracer = new StackTracer();
                       List<StackSample> stackSampleCollection = new List<StackSample>();
                       stackTracer.processName = Process.GetProcessById(pid).ProcessName;
                       stackTracer.processID = pid;

                       for (int i = 1; i <= stackTraceCount; i++)
                       {
                           Console.WriteLine("Collecting sample # {0} ", i);
                           StackTracerLogger.AppendLine("Collecting sample # "+ i);
                           // Instanting all the datastrcture to hold the 
                           //stactrace sample objects for each sample
                           StackSample stackTracerProcessobj = new StackSample();
                           stackTracerProcessobj.processThreadCollection = new List<Thread>();
                           stackTracerProcessobj.sampleCounter = i;
                           stackTracerProcessobj.samplingTime = DateTime.UtcNow;
                           

                           // Trying to attach the debugger to the selected process
                           using (DataTarget dataTarget = DataTarget.AttachToProcess(pid, 5000, AttachFlag.Invasive))
                           {
                               string dacLocation = string.Empty;
                               ClrRuntime runtime = null;
                               try { 
                                
                                dacLocation = dataTarget.ClrVersions[0].TryGetDacLocation();                                   
                                runtime = dataTarget.CreateRuntime(dacLocation);
                                   
                               }
                               catch(Exception ex)
                               {
                                  
                                   Console.WriteLine("CLR Runtime could not be initialized or process does not have CLR loaded");
                                   Console.WriteLine(ex.Message);                                   
                                   StackTracerLogger.AppendLine(ex.Message);
                                   throw ex;
                               }
                               stackTracerProcessobj.threadCount = runtime.Threads.Count;
                               StackTracerLogger.AppendLine("=============================================================================================================");
                               StackTracerLogger.AppendLine("There are" + runtime.Threads.Count + "threads in the" + Process.GetProcessById(pid).ProcessName + " process");
                               StackTracerLogger.AppendLine("=============================================================================================================");
                               StackTracerLogger.AppendLine();

                               foreach (ClrThread crlThreadObj in runtime.Threads)
                               {
                                   Thread stackTracerThreadObj = new Thread();
                                   List<StackFrame> tracerStackThread = new List<StackFrame>();
                                   IList<ClrStackFrame> Stackframe = crlThreadObj.StackTrace;
                                   stackTracerThreadObj.oSID = crlThreadObj.OSThreadId;
                                   stackTracerThreadObj.managedThreadId = crlThreadObj.ManagedThreadId;
                                   StackTracerLogger.AppendLine("There are " + crlThreadObj.StackTrace.Count + "  items in the stack for current thread ");
                                   foreach (ClrStackFrame stackFrame in Stackframe)
                                   {
                                       stackTracerThreadObj.sampleCaptureTime = DateTime.UtcNow;
                                       string tempClrMethod = "NULL";
                                       if (stackFrame.Method != null)
                                           tempClrMethod = stackFrame.Method.GetFullSignature(); // TO DO : We need to create a dicitonary.
                                       tracerStackThread.Add(new StackFrame(stackFrame.DisplayString, stackFrame.InstructionPointer, tempClrMethod, stackFrame.StackPointer));
                                   }
                                   stackTracerThreadObj.stackTrace = tracerStackThread;
                                   stackTracerProcessobj.processThreadCollection.Add(stackTracerThreadObj);
                               }
                           }
                           //Adding the stacktrace sample to the stack trace sample collecction.
                           stackSampleCollection.Add(stackTracerProcessobj);
                           //Delaying the next stack trace sample by {delay} seconds.
                           System.Threading.Thread.Sleep(delay);
                       }
                       //Pushing all the satckTrace samples in the global stacktracer object for serialization into xml
                       stackTracer.sampleCollection = stackSampleCollection;
                       Type testype = stackTracer.GetType();
                       //Calling function to serialize the stacktracer object.
                       ObjectSeralizer(stacktraceLocation, testype, stackTracer);
                       StackTracerLogger.AppendLine();
                   }                  
                   else
                   {
                       //Console.WriteLine("There are multiple process instances with selected process name  :  {0}",processName);
                       //Console.WriteLine("Use process ID for {0} process to capture the stack trace ", processName);
                       Console.WriteLine("Example: StackTracer.exe PID /d 5 /s 60 /i 500");
                   }
               }
            }
            catch (Exception ex)
            {

                if (ex != null && ex.StackTrace != null)
                {
                    StackTracerLogger.AppendLine("Exception Occured :" + ex.StackTrace.ToString());
                    if (state != ParseState.Help)
                        Console.WriteLine("Error Occured, refer Stacktracer.log located at {0} " , Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)));
                }
             }
           finally
            {
                if(StackTracerLogger.Length !=0)
                {
                    System.IO.File.WriteAllText(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),  "Stacktracer.log"), StackTracerLogger.ToString());
                    //Console.ReadLine();
                }
                
            }
        }
       /// <summary>
       /// Usage method is used to display the help menu to end user
       /// </summary>
       static void Usage()
       {
           Console.WriteLine();
           Console.WriteLine("Usage: StackTracer : ProcessName|PID [options] ");
           Console.WriteLine("--------------------------------------------------------------------------------");
           Console.WriteLine(" ProcessName|PID  : You can give .NET process name or Process ID (Default:W3Wp)");
           Console.WriteLine(" /D : Initial delay in seconds to halt trace collection (Default:0)");
           Console.WriteLine(" /S : Indicates number of StackTraces for the process. (Default:10)");
           Console.WriteLine(" /I : Interval between StackTrace samples in milliseconds (Default:1000)");
           Console.WriteLine(" /? : To get the usage menu");
           Console.WriteLine("-------------------------------------------------------------------------------");
           Console.WriteLine("Example: stacktracer w3wp /d 10 /s 60 /i 500");
           Console.WriteLine("Wait for 10 seconds to take 60  stacktrace samples for w3wp process...");
           Console.WriteLine("..where you are taking one stacktrace sample in every 500 milliseconds");
           //Console.Read();
       }
       private static bool IsWow64(Process process)
       {
           if ((Environment.OSVersion.Version.Major > 5)
               || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
           {
               try
               {
                   bool retVal;

                   return NativeMethods.IsWow64Process(process.Handle, out retVal) && retVal;
               }
               catch
               {
                   return false; // access is denied to the process
               }
           }

           return false; // not on 64-bit Windows
       }
       public static void ObjectSeralizer(String filePath, Type OjectType, object Object)
        {
             //Sample to get the file from the resource. 
             //Check if stacktrace.xsl already exixt in filepath location
             string tempfilepath = (System.Reflection.Assembly.GetExecutingAssembly().Location).Replace(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location),"")+ "stacktrace.xsl";

             Stream _textStreamReader = GetXSLFromAssembly();

            if (!File.Exists(tempfilepath))
            {
                
               using (Stream s = File.Create(tempfilepath))
               {                   
                  _textStreamReader.CopyTo(s);
               }
               
            }
            else
            {

                if (!FileHashChecked && Algorithms.GetChecksum(Algorithms.MD5,_textStreamReader)!=Algorithms.GetChecksum(tempfilepath,Algorithms.MD5))
                {
                    using (Stream s = File.Create(tempfilepath))
                    {
                        //resetting the position og stream
                        _textStreamReader.Position = 0;
                        _textStreamReader.CopyTo(s);
                    }
                }
            }
           
           //  Code to seralize the oject.
            string  stacktraceLocation = filePath;
             Type ClassToSerelaize = OjectType;
                if (string.IsNullOrEmpty(stacktraceLocation))
                    stacktraceLocation = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), DateTime.Now.Ticks + ".xml");          
                    Console.WriteLine("Generating the StackTrace report....");

                 //Serializing the result for the runtime to look into the full object
                XmlSerializer serializer = new XmlSerializer(ClassToSerelaize);
                using (XmlWriter writer = XmlWriter.Create(stacktraceLocation))
                            {
                                writer.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"stacktrace.xsl\"");
                                serializer.Serialize(writer, Object);
                            }
                            Console.WriteLine("StackTace Report Generated at the path :");
                            Console.WriteLine( stacktraceLocation);
                            // Console.Read();          
        }

       private static Stream GetXSLFromAssembly()
       {
           Assembly _assembly = Assembly.GetExecutingAssembly();
           Stream _textStreamReader = _assembly.GetManifestResourceStream("StackTracer.bin.Debug.stacktrace.xsl");
           return _textStreamReader;
       }              
    }
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
        public List<Thread> processThreadCollection { get; set; }
    }
    /// <summary>
    /// Thread object contains the information related to the current thread for which stack
    /// trace is being generated
    /// </summary>
    public class Thread
    {
        public Thread()
        {
        }
        public Thread(DateTime stackCaptureTime, List<StackFrame> stackTrace)
        {
            this.sampleCaptureTime = stackCaptureTime;
            this.stackTrace = stackTrace;
        }
        public DateTime sampleCaptureTime { get; set; }
        public int managedThreadId { get; set; }
        public uint oSID { get; set; }
        public List<StackFrame> stackTrace { get; set; }
    }
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
    public static class Algorithms
    {
        public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
        //public static readonly HashAlgorithm SHA1 = new SHA1Managed();
        //public static readonly HashAlgorithm SHA256 = new SHA256Managed();
        //public static readonly HashAlgorithm SHA384 = new SHA384Managed();
        //public static readonly HashAlgorithm SHA512 = new SHA512Managed();
        //public static readonly HashAlgorithm RIPEMD160 = new RIPEMD160Managed();


        public static string GetChecksum(string filePath, HashAlgorithm algorithm)
        {
            using (var stream = new BufferedStream(File.OpenRead(filePath), 100000))
            {
                return GetChecksum(algorithm, stream);
            }
        }

        public static string GetChecksum(HashAlgorithm algorithm, Stream stream)
        {
            byte[] hash = algorithm.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }

}

