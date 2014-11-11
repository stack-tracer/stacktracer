#StackTracer

<img src="http://debugging.io/images/stack.ico"
 alt="stacktracer logo" title="stacktracer" align="right" />

StackTracer is a command line tool that captures stack trace samples at fixed intervals from any .net CLR process.

This is a console application attaches and works with any .net process; no access to application source code is necessary,
no library modifications are needed, and there is no run-time instrumentation of CLR code. Configuration
options given at start of command line to specify the interval for stack trace and number of samples.

###Highlights

>*	small footprint (<1MB) single exe file
>*	Captures stack trace of any .net process Windows forms ,WPF, asp.net,windows service etc. 
>*	support for Microsoft Azure Websites (with the help of FREB)
>*	No need of any symbol files.
>*	Support for applications running in .net framework 2.0 to 4.5
>*	Target 32 bit and 64 bit process.
>*	View traces in browser. (inspired from IIS FREB)
>*	Intuitive timeline view to filter out unwanted stacks/threads.
>*	Very easy to troubleshoot Hang or High CPU issues on production enviroments.
>*	Less overhead on the running process,so you can even troubleshoot slow performance issues which are less than 3-4 seconds .	


###Installation

Just download and copy [stacktracer.exe](https://github.com/stack-tracer/stacktracer/releases/download/v1.0/StackTracer.zip) onto your server. Typing "stacktracer" displays its usage syntax.


####Usage

**StackTracer : ProcessName|PID [options]**


`ProcessName|PID  : You can give .NET process name or Process ID `

`/D : Delay in seconds to before starting capture.This will give you time to reproduce the scenario which takes time(Default:0)`

`/S : Samples to be captured- Indicates number of samples to be captured. (Default:10)`

`/I : Interval between StackTrace samples in milliseconds (Default:1000)`

`/? : To get this help menu`

`When giving process name,don't specify the process extension exe.The output will be generated as XML file on the same folder where stacktracer is run from.`


**stacktracer w3wp**

`This command captures stacktraces of all CLR .net threads running in w3wp.exe process for 10 seconds.It will generate the output as an xml file in the same folder as stacktracer.exe.`

**stacktracer /s 20 /i 500 iexplore**

`Above command captures 20 samples where each sample is 500 milli seconds apart from iexplore.exe process.`

	


### Supported runtimes

* Windows vista+/Windows server 2008+ with .net framework 4.0 client side profile installed
* works with any .net process targeted for .NET Framework 3.5/4.0/4.5

### Versions

* On premise environments 
*  for running in hosted environments (where you don't have permission to run as an administrator)

Each version comes with specific exe for 32 bit and 64 bit. Make sure the bitness of stacktracer and target process match.

### More examples




**stacktracer wpfapp**

`The above command captures 10 samples (a sample contains stack traces of all the .net threads running in the process at a particular point of time) from wpfapp.exe where each sample is 1s (1000 ms) apart`

**stacktracer w3wp /s 60 /i 500**

`capture 60 samples for w3wp process where each sample is captured every 500 milliseconds interval`

**stacktracer w3wp /d 10 /s 60 /i 500**

`Wait for 10 seconds to take 60 samples for w3wp process where each sample is captured every 500 milliseconds interval`


---------------------------------------------------------------------------------------------------------------

<br/>

	
