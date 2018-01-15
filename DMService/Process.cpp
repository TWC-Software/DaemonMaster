#include "stdafx.h"
#include "Process.h"

Process::Process(const ProcessStartInfo& psi)
{
	InitializeCriticalSection(&criticalSection);
	pInfo = psi;
}


Process::~Process()
{
	DeleteCriticalSection(&criticalSection);

	if (waitHandle)
	{
		//Wait until all callbacks are finished and then return
		UnregisterWaitEx(waitHandle, INVALID_HANDLE_VALUE);
		waitHandle = NULL;
	}

	CleanUp();
}

void Process::CleanUp()
{
	if (processHandle)
	{

		DWORD exitCode;
		if (GetExitCodeProcess(processHandle, &exitCode))
		{
			if (exitCode == STILL_ACTIVE)
				TerminateProcess(processHandle, 1);
		}

		CloseHandle(processHandle);
		processHandle = NULL;
	}

	if (threadHandle)
	{
		CloseHandle(threadHandle);
		threadHandle = NULL;
	}
}

void Process::UnregisterWaitHandleDirectly()
{
	if (waitHandle)
	{
		UnregisterWaitEx(waitHandle, NULL);
	}
}

bool Process::Start()
{
	if (pInfo.GetFullPath().empty())
		return false;

	return StartWithCreateProcess();	
}

bool Process::Stop()
{
	//TODO: Gracefuly
	if (processHandle)
		return TerminateProcess(processHandle, 0);

	return false;
}


bool Process::StartWithCreateProcess()
{
	DWORD creationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT;

	STARTUPINFOW si;
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);

	PROCESS_INFORMATION pi;
	ZeroMemory(&pi, sizeof(pi));

	const BOOL result = CreateProcessW(pInfo.GetFullPath().c_str(), _tcsdup(pInfo.GetParameters().c_str()), NULL, NULL, FALSE, creationFlags, NULL, pInfo.GetFileDir().c_str(), &si, &pi);

	if (result)
	{
		processHandle = pi.hProcess;
		threadHandle = pi.hThread;
		processId = pi.dwProcessId;

		RegisterExitCallback();
		return true;
	}

	return false;
}


bool Process::RegisterExitCallback()
{
	if (waitHandle || !processHandle)
		return false;

	return RegisterWaitForSingleObject(&waitHandle, processHandle, OnExitedCallback, this, INFINITE, WT_EXECUTEONLYONCE);
}

void CALLBACK Process::OnExitedCallback(PVOID params, BOOLEAN timerOrWaitFired)
{
	static_cast<Process*>(params)->CleanUp();
	static_cast<Process*>(params)->OnExited();
}

void Process::OnExited()
{
	EnterCriticalSection(&criticalSection);

	Start();

	LeaveCriticalSection(&criticalSection);
}

