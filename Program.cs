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
using System.Security.Principal;

namespace StackTracer
{ 
    
    class Program
    {
        private static bool _fileHashChecked = true;
        private static bool _hasCancelkeyPressed = false;
        private static int _processId = -1;
        private static StringBuilder _logger;
        
        enum Bitness
        {
            UnKnown,x86,x64
        }

       static void Main(string[] args)
        {
            
            Bitness currentProcessBitness, targetProcessBitness;
            RunOptions options=null;
            string traceLocation = null;
            _logger = new StringBuilder();
            string arguments = string.Empty;
            bool returnFromConsole = false;

           try
            {
                
                
                
                

                //add process identity information for logging   
                LogUserDetails();
                
                
                // Getting the parameters initialized 
                 options= ParseArguments(args, out returnFromConsole);
                

                //return from main if passed parameters are not correct.
                if (returnFromConsole)
                {
                    return;
                }
                Process targetProcess = null;
                
                //Console.CancelKeyPress += Console_CancelKeyPress;

                if (options.State != ParseState.Help)//if arguments are ok.
               {
                   targetProcess = CheckProcessExists(_logger, options.ProcessName, options.PID);
                  
                   if (targetProcess!=null)
                   {
                       //verify if the processbitness and stacktracer bitness matches
                       CheckProcessBitness(_logger, targetProcess, out currentProcessBitness, out targetProcessBitness);

                       if (options.RunAsRemote)
                       {
                           RunAsRemote(_logger, arguments,  targetProcess);
                           return;                           
                       }
                       

                       if (options.InitialDelay>0)
                       {
                           Console.WriteLine("Initiating the stacktrace capture after given delay of {0} seconds....", options.InitialDelay);
                           System.Threading.Thread.Sleep(options.InitialDelay * 1000);
                       }                  
                       

                       //wrapper class for holding trace xml
                       StackCaptures traceCaptures = new StackCaptures(targetProcess.ProcessName,targetProcess.Id);
                       //List<StackSample> stackSampleCollection = new List<StackSample>();
                       
                       //dicttionary to speed up stackframe calculation
                       //if an Instruction Pointer is already found,don't recalculate the methodstring.
                       Dictionary<ulong, string> ipToMethodMap = new Dictionary<ulong, string>();
                       

                       for (int i = 1; i <= options.SamplesCount; i++)
                       {
                           
                           // Instanting all the datastrcture to hold the 
                           //stactrace sample objects for each sample
                           StackSample currentSample = new StackSample();
                           currentSample.processThreadCollection = new List<STThread>();
                           currentSample.sampleCounter = i;
                           currentSample.samplingTime = DateTime.UtcNow;
                           

                           // Trying to attach the debugger to the selected process
                           using (DataTarget dataTarget = DataTarget.AttachToProcess(_processId, 5000, AttachFlag.Invasive))
                           {
                               Console.WriteLine("Collecting sample # {0} ", i);
                               _logger.AppendLine("Collecting sample # " + i);

                               string dacLocation = string.Empty;
                               ClrRuntime runtime = null;
                               try 
                               { 
                               
                                 //sometimes,dac cannot be found
                                dacLocation = dataTarget.ClrVersions[0].TryGetDacLocation();
                                if (string.IsNullOrEmpty(dacLocation))
                                {
                                    dacLocation = TryFixDacLocation(_logger, targetProcessBitness, dataTarget, dacLocation);
                                }
                               
                                   //logging clr version for debugging purpose
                                _logger.AppendLine("CLR Version: " + dataTarget.ClrVersions[0].DacInfo.FileName + dataTarget.ClrVersions[0].DacInfo.ImageBase);
                                
                                   
                                //create  clrruntime
                                runtime = dataTarget.CreateRuntime(dacLocation);
                                   
                                   
                               }
                               catch(Exception ex)
                               {
                                  
                                   Console.WriteLine("CLR Runtime could not be initialized or process does not have CLR loaded");
                                   Console.WriteLine(ex.Message);                                   
                                   _logger.AppendLine(ex.Message);
                                   throw ex;
                               }
                               currentSample.threadCount = runtime.Threads.Count;
                               _logger.AppendLine("=============================================================================================================");
                               _logger.AppendLine("There are" + runtime.Threads.Count + "threads in the" + targetProcess.ProcessName + " process");
                               _logger.AppendLine("=============================================================================================================");
                               _logger.AppendLine();

                               foreach (ClrThread crlThreadObj in runtime.Threads)
                               {
                                   STThread currentCLRThread = new STThread();
                                  
                                   List<StackTracer.Utils.StackFrame> stackFrames = new List<StackTracer.Utils.StackFrame>();

                                   //IList<ClrStackFrame> Stackframe = crlThreadObj.StackTrace;
                                   currentCLRThread.oSID = crlThreadObj.OSThreadId;

                                   if (!crlThreadObj.IsGC && !crlThreadObj.IsFinalizer && !crlThreadObj.IsSuspendingEE && !crlThreadObj.IsDebuggerHelper)
                                   {
                                       currentCLRThread.managedThreadId = crlThreadObj.ManagedThreadId;
                                       _logger.AppendLine("There are " + crlThreadObj.StackTrace.Count + "  items in the stack for current thread ");

                                       var clrroots = crlThreadObj.EnumerateStackObjects();


                                       foreach (var context in clrroots)
                                       {
                                           if (context.Type.Name == "System.Web.HttpContext")
                                           {
                                               currentCLRThread.HasHttpContext = true;
                                               if (context.Type.HasSimpleValue)
                                               {
                                                   var timestamp = context.Type.GetFieldByName("_utcTimestamp");
                                                   var timestampAdd = timestamp.GetFieldAddress(context.Address);
                                                   var dateData = timestamp.Type.GetFieldByName("dateData");
                                                   var ticks = dateData.GetFieldValue(timestampAdd, true);
                                                   currentCLRThread.RequestTimeStamp = new DateTime((long)((ulong)ticks & 4611686018427387903uL));
                                                   var request = context.Type.GetFieldByName("_request");
                                                   ulong reqAddress = (ulong)request.GetFieldValue(context.Object);
                                                   var url = request.Type.GetFieldByName("_rawUrl");
                                                   currentCLRThread.RequestUrl = (string)url.GetFieldValue(reqAddress);
                                               }
                                           }

                                       }
                                       currentCLRThread.sampleCaptureTime = DateTime.UtcNow;
                                       foreach (ClrStackFrame stackFrame in crlThreadObj.StackTrace)
                                       {

                                           if (stackFrame.Kind == ClrStackFrameType.ManagedMethod)
                                           {
                                               string tempClrMethod = "NULL";
                                               if (stackFrame.Method != null)
                                               {
                                                   //to improve performance to get the stacktrace string 
                                                   if (!ipToMethodMap.ContainsKey(stackFrame.InstructionPointer))
                                                   {
                                                       tempClrMethod = stackFrame.Method.GetFullSignature();
                                                       ipToMethodMap.Add(stackFrame.InstructionPointer, tempClrMethod);
                                                   }
                                                   else
                                                       tempClrMethod = ipToMethodMap[stackFrame.InstructionPointer];
                                                   stackFrames.Add(new StackTracer.Utils.StackFrame(stackFrame.DisplayString, stackFrame.InstructionPointer, tempClrMethod, stackFrame.StackPointer));
                                               }
                                           }

                                       }
                                       currentCLRThread.stackTrace = stackFrames;
                                       currentSample.processThreadCollection.Add(currentCLRThread); 
                                   }
                               }
                           }
                           //Adding the stacktrace sample to the stack trace sample collecction.
                           traceCaptures.AddSample(currentSample);
                           //Delaying the next stack trace sample by {delay} seconds.
                           System.Threading.Thread.Sleep(options.Interval);
                           if (_hasCancelkeyPressed)
                           {
                               _logger.AppendLine("Detected cancel key press,aborting trace capture");
                               Console.WriteLine("Detected cancel key press,aborting trace capture");
                               break;
                           }
                       }
                       //Pushing all the satckTrace samples in the global stacktracer object for serialization into xml
                       //traceCapture.Samples = stackSampleCollection;
                       Type testype = traceCaptures.GetType();
                       //Calling function to serialize the stacktracer object.
                       ObjectSeralizer(traceLocation, testype, traceCaptures);
                       _logger.AppendLine();
                   }                  
                   else
                   {
                       //Console.WriteLine("There are multiple process instances with selected process name  :  {0}",processName);
                       //Console.WriteLine("Use process ID for {0} process to capture the stack trace ", processName);
                       Console.WriteLine("Example: StackTracer.exe PID /s 60 /i 500");
                       Console.WriteLine("StackTracer.exe w3wp /d 5 /s 60 /i 500");
                       Console.WriteLine("StackTracer.exe PID /d 5 /s 60 /i 500");
                       
                       
                   }
               }//end of if  arguments are corret
            }
            catch (Exception ex)
            {

                if (ex != null && ex.StackTrace != null)
                {
                    _logger.AppendLine("Exception Occured :" + ex.StackTrace.ToString());
                    if (options.State != ParseState.Help)
                        Console.WriteLine("Error Occured, refer Stacktracer.log located at {0} " , Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)));
                }
             }
           finally
            {
                if(_logger.Length !=0)
                {
                    System.IO.File.WriteAllText(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),  "Stacktracer.log"), _logger.ToString());
                    //Console.ReadLine();
                }
                _hasCancelkeyPressed = false;
                
            }
        }

       private static string TryFixDacLocation(StringBuilder stbLogger, Bitness targetProcessBitness, DataTarget dataTarget, string dacLocation)
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

           stbLogger.AppendLine("DacLocation: " + dacLocation);
           return dacLocation;
       }

       private static void RunAsRemote(StringBuilder stbLogger, string arguments, Process targetProcess)
       {
           stbLogger.AppendLine("Begining to inject remote thread");
           string appname = Assembly.GetExecutingAssembly().Location;
           stbLogger.AppendFormat("target procid:{0}appname:{1}arguments{2}", targetProcess.Id, appname, arguments.Replace("/c", ""));

           Injector.InjectAndRun(targetProcess.Id, appname, arguments.Replace("/c", ""));
          
       }

       private static void CheckProcessBitness(StringBuilder stbLogger, Process targetProcess, out Bitness currentProcessBitness, out Bitness targetProcessBitness)
       {
           Exception exBitnessCheck = null;
           if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess)
           {
               currentProcessBitness = Bitness.x64;
           }
           else
               currentProcessBitness = Bitness.x86;

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
           if (exBitnessCheck != null)
           {
               stbLogger.AppendLine("Bitness check for target process failed");
               stbLogger.Append(exBitnessCheck.Message);
           }
       }

       private static Process CheckProcessExists(StringBuilder stbLogger, string processName, int Pid)
       {
           Process targetProcess = null;
           if (string.IsNullOrEmpty(processName))
           {
               _processId = Pid;
               try
               {
                   targetProcess = Process.GetProcessById(_processId);

               }
               catch (Exception ex)
               {
                   Console.WriteLine("Could not find process with PID {0} ", _processId);
                   stbLogger.AppendLine(ex.Message);
               }
           }
           else
           {
               try
               {
                   //check for multiple process with same names
                   Process[] processes = Process.GetProcessesByName(processName);
                   if (processes.Length == 0)
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
                       _processId = targetProcess.Id;
                   }
               }
               catch (Exception ex)
               {
                   if (processName != "")
                       Console.WriteLine("Process with name {0} Doesn't Exist", processName);
                   else
                   {

                   }
                   stbLogger.AppendLine("Process with name" + processName + " doesn't exist");
                   throw ex;

               }
           }
           return targetProcess;
       }

       private static RunOptions ParseArguments(string[] args, out bool returnFromConsole)
       {
           //processname if passed as arguments
           string processName = "";
           //processid if passed as argument
           int pId = -1;
           //default delay 
           int delay = 500;
           //default sampleCount
           int sampleCount = 10;
           
           //will hold delay passed as argument
           int pdelay = 0;
           int Pid;
           //if this falg is set,we will use createremotethread to inject dll into target process(can be used in waws enviroment)
           bool runAsRemote = false;
                   
           var state = ParseState.Unknown;

           returnFromConsole = false;
           //if no arguments are paased ,show help menu
           if (args != null && args.Length > 1)
           {
               _logger.AppendLine("Launching StackTracer.exe with params");
               string arguments = string.Join(" ", args);
               _logger.AppendLine(arguments);

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
                           else if (arg.ToLower() == "/c")
                           {
                               state = ParseState.RunAsChild;
                               runAsRemote = true;
                           }
                           else
                           {
                               ShowHelp();
                               state = ParseState.Help;
                               returnFromConsole = true;
                           }
                           break;
                       case ParseState.Samples:
                           if (!int.TryParse(arg, out sampleCount))
                           {
                               Console.WriteLine("Unable to parse sample count");
                               _logger.AppendLine("Unable to parse sample count");
                               ShowHelp();
                               state = ParseState.Help;
                               returnFromConsole = true;
                           }
                           state = ParseState.Unknown;
                           break;
                       case ParseState.Interval:
                           if (!int.TryParse(arg, out delay))
                           {
                               Console.WriteLine("Unable to parse interval value");
                               _logger.AppendLine("Unable to parse interval value");
                               ShowHelp();
                               state = ParseState.Help;
                               returnFromConsole = true;
                           }
                           state = ParseState.Unknown;
                           break;
                       case ParseState.Predelay:
                           if (!int.TryParse(arg, out pdelay))
                           {
                               Console.WriteLine("Unable to parse pre-delay");
                               _logger.AppendLine("Unable to parse pre-delay");
                               ShowHelp();
                               state = ParseState.Help;
                               returnFromConsole = true;
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

           return new RunOptions {InitialDelay=pdelay,Interval=delay,PID=pId,ProcessName=processName,RunAsRemote=runAsRemote,SamplesCount=sampleCount,State=state };
       }

       private static void LogUserDetails()
       {
           _logger.AppendLine("Initializing StackTracer.exe");
           WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
           if (currentIdentity != null)
           {
               _logger.AppendLine("Current Identity is " + currentIdentity.Name);
               _logger.AppendLine("User: " + currentIdentity.User.Value);
               _logger.AppendLine("Owner: " + currentIdentity.Owner.Value);
               _logger.AppendLine("Is System: " + currentIdentity.IsSystem.ToString());
               _logger.AppendLine("Is Guest: " + currentIdentity.IsGuest.ToString());
               _logger.AppendLine("Is Authenticated" + currentIdentity.IsAuthenticated.ToString());
               _logger.AppendLine("Is Anonymous: " + currentIdentity.IsAnonymous.ToString());
               _logger.AppendLine("ImpersonationLevel: " + currentIdentity.ImpersonationLevel.ToString());
               _logger.AppendLine("Authentication Type: " + currentIdentity.AuthenticationType.ToString());
               _logger.AppendLine("Apartment State: " + System.Threading.Thread.CurrentThread.GetApartmentState().ToString());
           }
       }

       static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
       {
           _hasCancelkeyPressed = true;
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

                if (!_fileHashChecked && Algorithms.GetChecksum(Algorithms.MD5,_textStreamReader)!=Algorithms.GetChecksum(tempfilepath,Algorithms.MD5))
                {
                    _fileHashChecked = true;
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

