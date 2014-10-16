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
using StackTracer.Utils;

namespace StackTracer
{ 
    
    class Program
    {
        private static bool FileHashChecked = false;
        private static bool HasCancelkeyPressed = false;
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
                    StackTracerLogger.AppendLine("Launching StackTracer.exe with params");
                    StackTracerLogger.AppendLine(string.Join("", args));
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
                                    ShowHelp();
                                    state = ParseState.Help;
                                    return;
                                }
                                break;
                            case ParseState.Samples:
                                if (!int.TryParse(arg, out stackTraceCount))
                                {
                                    Console.WriteLine("Unable to parse sample count");
                                    StackTracerLogger.AppendLine("Unable to parse sample count");
                                    ShowHelp();
                                    state = ParseState.Help;
                                    return;
                                }
                                state = ParseState.Unknown;
                                break;
                            case ParseState.Interval:
                                if (!int.TryParse(arg, out delay))
                                {
                                    Console.WriteLine("Unable to parse interval value");
                                    StackTracerLogger.AppendLine("Unable to parse interval value");
                                    ShowHelp();
                                    state = ParseState.Help;
                                    return;
                                }
                                state = ParseState.Unknown;
                                break;
                            case ParseState.Predelay:
                                if (!int.TryParse(arg, out pdelay))
                                {
                                    Console.WriteLine("Unable to parse pre-delay");
                                    StackTracerLogger.AppendLine("Unable to parse pre-delay");
                                    ShowHelp();
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
                                ShowHelp();
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
                    ShowHelp();
                    state = ParseState.Help;


                }
                #endregion

                Process targetProcess = null;
                
                //Console.CancelKeyPress += Console_CancelKeyPress;
               if (state != ParseState.Help)
               {
                   if (string.IsNullOrEmpty(processName))
                   {
                       pid = Pid;
                       try
                       {
                           targetProcess = Process.GetProcessById(pid);

                       }
                       catch(Exception ex)
                       {
                           Console.WriteLine("Could not find process with PID {0} ", pid);
                           StackTracerLogger.AppendLine(ex.Message);
                       }
                   }
                   else
                   {
                       try
                       {
                           //check for multiple process with same names
                           Process[] processes = Process.GetProcessesByName(processName);
                           if (processes.Length==0)
                           {
                               Console.WriteLine("Could not find process with name {0}", processName);
                               Console.WriteLine("Please verify if " + processName + " process is currently running");
                               Console.WriteLine();

                           }
                           if (processes.Length > 1)
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

                           }
                           StackTracerLogger.AppendLine("Process with name" + processName + " doesn't exist");
                           throw ex;

                       }
                   }
                   if (targetProcess!=null)
                   {
                       Exception exBitnessCheck = null;
                       if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess)
                       {
                           currentProcessBitness=Bitness.x64;
                       }
                       else
                           currentProcessBitness=Bitness.x86;

                       if (Environment.Is64BitOperatingSystem && !IsWow64(targetProcess, out exBitnessCheck))
                       {
                           targetProcessBitness = Bitness.x64;
                       }
                       else
                       {
                           targetProcessBitness = Bitness.x86;
                       }

                       if (exBitnessCheck == null && currentProcessBitness != targetProcessBitness)
                       {

                           string errorMessage = string.Format("Process bitness mismatch!, Stacktracer.exe is of {0} and Target process {2} is of {1}. Please use {3} StackTracer.exe", currentProcessBitness == Bitness.x64 ? "64 bit" : "32 bit",
                               targetProcessBitness == Bitness.x64 ? "64 bit" : "32 bit", targetProcess.ProcessName, currentProcessBitness == Bitness.x64 ? "32 bit" : "64 bit");
                           Console.WriteLine(errorMessage);
                           throw new Exception(errorMessage);
                       }
                       if (exBitnessCheck!=null)                      
                       {
                           StackTracerLogger.AppendLine("Bitness check for target process failed");
                           StackTracerLogger.Append(exBitnessCheck.Message);
                       }
                       if (pdelay>0)
                       {
                           Console.WriteLine("Initiating the stacktrace capture after given delay of {0} seconds....", pdelay);
                       }
                       
                       
                       System.Threading.Thread.Sleep(pdelay * 1000);
                       if (HasCancelkeyPressed)
                       {
                           StackTracerLogger.AppendLine("Detected cancel key press,aborting trace capture");
                           Console.WriteLine("Detected cancel key press,aborting trace capture");
                       }
                       //StackTracerLogger.AppendLine("The selected pid for the process " + processName + " is " + pid);
                       StackTracer.Utils.StackTracer stackTracer = new StackTracer.Utils.StackTracer();
                       List<StackSample> stackSampleCollection = new List<StackSample>();
                       stackTracer.processName = Process.GetProcessById(pid).ProcessName;
                       stackTracer.processID = pid;

                       for (int i = 1; i <= stackTraceCount; i++)
                       {
                           
                           // Instanting all the datastrcture to hold the 
                           //stactrace sample objects for each sample
                           StackSample stackTracerProcessobj = new StackSample();
                           stackTracerProcessobj.processThreadCollection = new List<Thread>();
                           stackTracerProcessobj.sampleCounter = i;
                           stackTracerProcessobj.samplingTime = DateTime.UtcNow;
                           

                           // Trying to attach the debugger to the selected process
                           using (DataTarget dataTarget = DataTarget.AttachToProcess(pid, 5000, AttachFlag.Invasive))
                           {
                               Console.WriteLine("Collecting sample # {0} ", i);
                               StackTracerLogger.AppendLine("Collecting sample # " + i);

                               string dacLocation = string.Empty;
                               ClrRuntime runtime = null;
                               try { 
                                
                                dacLocation = dataTarget.ClrVersions[0].TryGetDacLocation();
                                if (string.IsNullOrEmpty(dacLocation))
                                {
                                    //to get this working in azure websites enviroment
                                    //try the path directly
                                    string mscordacwksFilename = dataTarget.ClrVersions[0].DacInfo.FileName;

                                    if (mscordacwksFilename.Contains("mscordacwks_x86_X86_4.0."))
                                    {
                                        if (targetProcessBitness == Bitness.x86)
                                        {
                                            dacLocation = @"D:\Windows\Microsoft.NET\Framework\v4.0.30319\mscordacwks.dll";
                                        }
                                        else
                                            dacLocation = @"D:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscordacwks.dll";
                                    }
                                    else
                                    {
                                        if (targetProcessBitness == Bitness.x86)
                                        {
                                            dacLocation = @"D:\Windows\Microsoft.NET\Framework\v2.0.50727\mscordacwks.dll";
                                        }
                                        else
                                            dacLocation = @"D:\Windows\Microsoft.NET\Framework64\v2.0.50727\mscordacwks.dll";

                                    }

                                    StackTracerLogger.AppendLine("DacLocation: " + dacLocation);
                                }
                               
                                StackTracerLogger.AppendLine("CLR Version: " + dataTarget.ClrVersions[0].DacInfo.FileName + dataTarget.ClrVersions[0].DacInfo.ImageBase);
                                


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
                                   List<StackTracer.Utils.StackFrame> tracerStackThread = new List<StackTracer.Utils.StackFrame>();
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
                                       tracerStackThread.Add(new StackTracer.Utils.StackFrame(stackFrame.DisplayString, stackFrame.InstructionPointer, tempClrMethod, stackFrame.StackPointer));
                                   }
                                   stackTracerThreadObj.stackTrace = tracerStackThread;
                                   stackTracerProcessobj.processThreadCollection.Add(stackTracerThreadObj);
                               }
                           }
                           //Adding the stacktrace sample to the stack trace sample collecction.
                           stackSampleCollection.Add(stackTracerProcessobj);
                           //Delaying the next stack trace sample by {delay} seconds.
                           System.Threading.Thread.Sleep(delay);
                           if (HasCancelkeyPressed)
                           {
                               StackTracerLogger.AppendLine("Detected cancel key press,aborting trace capture");
                               Console.WriteLine("Detected cancel key press,aborting trace capture");
                               break;
                           }
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
                       Console.WriteLine("Example: StackTracer.exe PID /s 60 /i 500");
                       Console.WriteLine("StackTracer.exe w3wp /d 5 /s 60 /i 500");
                       Console.WriteLine("StackTracer.exe PID /d 5 /s 60 /i 500");
                       
                       
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
                HasCancelkeyPressed = false;
                
            }
        }

       static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
       {
           HasCancelkeyPressed = true;
           e.Cancel = true;
       }
       /// <summary>
       /// Usage method is used to display the help menu to end user
       /// </summary>
       static void ShowHelp()
       {
           Console.WriteLine();
           Console.WriteLine("StackTracer Version 1.0");
           Console.WriteLine();
           Console.WriteLine("Usage: StackTracer ProcessName|PID [options] ");
           Console.WriteLine("--------------------------------------------------------------------------------");
           Console.WriteLine(" ProcessName|PID  : .NET process name or Process ID (Default:W3Wp)");
           Console.WriteLine(" /D : Delay in seconds to before starting capture.This will give you time to reproduce the issue (Default:0)");
           Console.WriteLine(" /S : Samples to be captured- Indicates number of samples to be captured. (Default:10)");
           Console.WriteLine(" /I : Interval between StackTrace samples in milliseconds (Default:1000)");
           Console.WriteLine(" /? : To get this help menu");
           Console.WriteLine("-------------------------------------------------------------------------------");
           Console.WriteLine("Examples:");
           Console.WriteLine("stacktracer w3wp /d 10 /s 60 /i 500");
           Console.Write("Wait for 10 seconds to take 60 samples for w3wp process ");
           Console.WriteLine("where each sample is captured every 500 milliseconds interval");
           Console.WriteLine("stacktracer 5688  /s 60 /i 500");
           Console.Write("Capture 60 samples for process with id 5688 ");
           Console.WriteLine("where each sample is captured every 500 milliseconds interval");
           
           //Console.Read();
       }
       private static bool IsWow64(Process process,out Exception exception)
       {
           exception=null;
           if ((Environment.OSVersion.Version.Major > 5)
               || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
           {
               try
               {
                   bool retVal;

                   return NativeMethods.IsWow64Process(process.Handle, out retVal) && retVal;
               }
               catch(Exception  ex)
               {
                   exception = ex;
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
                    FileHashChecked = true;
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
    
    
   
    
   
   

}

