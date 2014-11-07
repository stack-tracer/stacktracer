#Introduction to StackTracer

<img src="http://debugging.io/images/stack.ico"
 alt="stacktracer logo" title="stacktracer" align="right" />

StackTracer is a  tool that captures and analyzes stack trace samples captured at fixed intervals from any .net CLR process.

This is a console application attaches using the debugengine Interface and works with any .net process; no access to application source code is necessary,
no library modifications are needed, and there is no run-time instrumentation of CLR code. Configuration
options given at start of command line to specify the interval for stack trace and number of samples.Current implementation include output generation in xml/xslt for viewing the most recent stack traces. The performance impact of the stacktracer is minimal. Future plans include GUI, automated data capturing.	


##Highlights

>*	800 kb single exe file(no installation required) with only dependency of .net framework 4.0 client profile.
>*	Captures stack trace of any .net process Windows forms ,WPF, asp.net you name it. 
>*	Works on Microsoft azure websites
>*	No need of any symbol files.
>*	You can troubleshoot applications written in .net framework 2.0 to 4.5
>*	Target 32 bit and 64 bit process.
>*	View traces in browser. (inspired from IIS FREB)
>*	Intuitive timeline view to filter out unwanted stacks/threads.
>*	Very easy to troubleshoot Hang or High CPU issues on production enviroments.
>*	Less overhead on the running process,so you can even troubleshoot slow performance issues which are less than 3-4 seconds .	


##Limitations

>*	Needs .NET framework 4.0
>*	Will not show any native stack information.
>*	Does not work on windows 2003	

##Downloads
<a href="https://github.com/stack-tracer/stacktracer/releases">Go to Release Page </a> and Download Stack Tracer.

---------------------------------------------------------------------------------------------------------------

<br/>

	
