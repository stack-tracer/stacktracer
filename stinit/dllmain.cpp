// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "windows.h"
#include "stdlib.h"
#include "string.h"
#include "stinit.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;
BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	
		    WCHAR   DllPath[MAX_PATH] = {0};
			
			PROCESS_INFORMATION processInformation={0};
			STARTUPINFO startupInfo={0};
			/*memset(&processInformation, 0, sizeof(processInformation));
			memset(&startupInfo, 0, sizeof(startupInfo));
			startupInfo.cb = sizeof(startupInfo);	*/	
			wchar_t *pdest;
			WCHAR cmdline[MAX_PATH*2]={0};
			WCHAR procID[10];   
		   int  result;
			HANDLE sm_hCustomActionThreadToken;

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
			GetModuleFileNameW((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));
			//pdest = strstr( string, str );
			 pdest= wcsstr(DllPath,L"stinit.dll");
			result = (int)(pdest - DllPath + 1);

		   if ( pdest != NULL )
		   {

			       
 
    if(RevertToSelf())  
 
    {  
				wcsncpy_s(cmdline,DllPath,result-1);
			   wcscat_s(cmdline,L"stacktracer.exe");	   
				wsprintfW(procID, L" %d", GetCurrentProcessId());
				bool result = CreateProcessW(cmdline, procID, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, NULL, &startupInfo, &processInformation);
 
    } 
		   }
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	
   
 //   bool result = ::CreateProcessW(L"", L"", NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL, NULL, &startupInfo, &processInformation);
    
/*
    if (result == 0)
    {
        wprintf(L"ERROR: CreateProcess failed!");
    }
    else
    {
        WaitForSingleObject( processInformation.hProcess, INFINITE );
        CloseHandle( processInformation.hProcess );
        CloseHandle( processInformation.hThread );
    }*/

	return TRUE;
}

