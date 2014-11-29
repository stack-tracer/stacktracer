// stinit.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"


extern "C"
{
	__declspec(dllexport) void ExecuteStackTracer(LPWSTR lpApplicationname, LPWSTR commandline)
  {
    PROCESS_INFORMATION processInformation;
    STARTUPINFO startupInfo;
    memset(&processInformation, 0, sizeof(processInformation));
    memset(&startupInfo, 0, sizeof(startupInfo));
    startupInfo.cb = sizeof(startupInfo);

    //BOOL result;
    //TCHAR tempCmdLine[MAX_PATH * 2];  //Needed since CreateProcessW may change the contents of CmdLine
   
	/*int length=wcslen(DllPath);
	
	int index=wcscmp(DllPath,L"stinit.dll");*/
	
	bool result = ::CreateProcessW(lpApplicationname, commandline, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, NULL, &startupInfo, &processInformation);
    
  }
}