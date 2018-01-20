//  DaemonMaster: Process
//  
//  This file is part of DeamonMaster.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////
#include "stdafx.h"
#include "Process.h"
#include "HandleData.h"
#include "ctime"
#include "WtsApi32.h"
#include "Functions.h"
#include <VersionHelpers.h>
#include <minwinbase.h>

Process::Process()
{
	//Enable SE_TCB_NAME privilege for WTSQueryUserToken
	Functions::EnablePrivilege(SE_TCB_NAME);
}

Process::~Process()
{
	CleanUp();
}


void Process::CleanUp()
{
	StopWatchingForExit();

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

	if(autoKillJobHandle)
	{
		CloseHandle(autoKillJobHandle);
		autoKillJobHandle = NULL;
	}
}


void Process::SetProcessStartInfo(const ProcessStartInfo& psi)
{
	if(!IsRunning())
		pInfo = psi;
}

void Process::SetStartMode(bool startInUserSession)
{
	this->startInUserSession = startInUserSession;
}

bool Process::Start()
{
	CleanUp();

	if (pInfo.GetFullPath().empty())
		return false;

	if(startInUserSession)
	{
		return StartOnActivUserSession();
	}
	else
	{
		return StartWithCreateProcess();
	}
}

bool Process::Stop()
{
	if (!IsRunning())
		return true;

	StopWatchingForExit();

	bool result;
	if(!pInfo.GetIsConsoleApp())
	{
		//result = PostMessageW(GetMainWindowHandle(), WM_CLOSE, NULL, NULL);
		result = SendMessageW(GetMainWindowHandle(), WM_SYSCOMMAND, SC_CLOSE, NULL);

		if (!result)
		{
			return Kill();
		}
	}
	else
	{
		result = GenerateConsoleCtrlEvent(!pInfo.GetUseCtrlC(), processId);

		if (!result)
		{
			return Kill();
		}
	}

	if (WaitForSingleObject(processHandle, 10000) == WAIT_TIMEOUT)
	{
		return Kill();
	}

	CleanUp();
	return true;
}

bool Process::Kill()
{
	if (!IsRunning())
		return true;

	if(TerminateProcess(processHandle, 0))
	{
		CleanUp();
		return true;
	}

	return false;
}


bool Process::StartWithCreateProcess()
{
	DWORD creationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT | CREATE_BREAKAWAY_FROM_JOB;

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

		time(&lastRestart);
		StartWatchingForExit();
		AssignAutoKillJob();
		return true;
	}

	return false;
}

bool Process::StartOnActivUserSession()
{
	DWORD creationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT | CREATE_BREAKAWAY_FROM_JOB;

	STARTUPINFOW si;
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	//si.lpDesktop = (LPWSTR) L"WinSta0\Default";
	si.wShowWindow = true;

	PROCESS_INFORMATION pi;
	ZeroMemory(&pi, sizeof(pi));


	//Get the physical console session (mouse,etc)
	ULONG sessionId = WTSGetActiveConsoleSessionId();

	HANDLE userToken = NULL;
	if (!WTSQueryUserToken(sessionId, &userToken))
		return false;


	const BOOL result = CreateProcessAsUserW(userToken, pInfo.GetFullPath().c_str(), _tcsdup(pInfo.GetParameters().c_str()), NULL, NULL, FALSE, creationFlags, NULL, pInfo.GetFileDir().c_str(), &si, &pi);

	CloseHandle(userToken);

	if (result)
	{
		processHandle = pi.hProcess;
		threadHandle = pi.hThread;
		processId = pi.dwProcessId;

		time(&lastRestart);
		StartWatchingForExit();
		AssignAutoKillJob();
		return true;
	}

	return false;
}


void Process::StopWatchingForExit()
{
	if (waitHandle)
	{
		UnregisterWait(waitHandle);
		waitHandle = NULL;
	}
}

void Process::StartWatchingForExit()
{
	if (waitHandle || !processHandle)
		return;

	RegisterWaitForSingleObject(&waitHandle, processHandle, OnExitedCallback, this, INFINITE, WT_EXECUTEONLYONCE);
}

bool Process::AssignAutoKillJob()
{
	if (!IsRunning())
		return false;

	BOOL processInJob;
	if (!IsProcessInJob(processHandle, NULL, &processInJob))
	{
		//FAILED
		return false;
	}

	if(processInJob && !IsWindows8OrGreater())
	{
		//Only one job is in windows 7 and lower allowed
		return false;
	}

	autoKillJobHandle = CreateJobObjectW(NULL, L"AutoKillJob");
	if(!autoKillJobHandle)
	{
		//FAILED
		return false;
	}

	JOBOBJECT_EXTENDED_LIMIT_INFORMATION jeli;
	ZeroMemory(&jeli, sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));

	jeli.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
	if(!SetInformationJobObject(autoKillJobHandle, JobObjectExtendedLimitInformation, &jeli, sizeof(jeli)))
	{
		CloseHandle(autoKillJobHandle);
		return false;
	}

	if(!AssignProcessToJobObject(autoKillJobHandle, processHandle))
	{
		CloseHandle(autoKillJobHandle);
		return false;
	}

	return true;
}


bool Process::IsRunning() const
{
	return processHandle != NULL && processHandle != INVALID_HANDLE_VALUE;
}


void CALLBACK Process::OnExitedCallback(PVOID params, BOOLEAN timerOrWaitFired)
{
	static_cast<Process*>(params)->CleanUp();
	static_cast<Process*>(params)->OnExited();
}

void Process::OnExited()
{
	if(pInfo.GetMaxRestartsResetTime() != 0 && difftime(TimeNow(),lastRestart) > pInfo.GetMaxRestartsResetTime())
	{
		restarts = 0;
	}

	if (pInfo.GetUnlimitedRestarts() || pInfo.GetMaxRestarts() > restarts)
	{
		restarts++;
		time(&lastRestart);

		Start();
	}
	else
	{
		CleanUp();
	}
}



HWND Process::GetMainWindowHandle() const
{
	HandleData data;
	data.pid = processId;

	EnumWindows(EnumWindowsCallback, reinterpret_cast<LPARAM>(&data));
	return data.bestHandle;
}

BOOL Process::EnumWindowsCallback(HWND hwnd, LPARAM lParam)
{
	HandleData& data = *reinterpret_cast<HandleData*>(lParam);

	DWORD pid;
	GetWindowThreadProcessId(hwnd, &pid);
	if(data.pid == pid && IsMainWindow(hwnd))
	{
		data.bestHandle = hwnd;
		//Stop the enumerating
		return false;
	}

	//continue
	return true;
}

BOOL Process::IsMainWindow(HWND hwnd)
{
	return GetWindow(hwnd, GW_OWNER) == NULL || IsWindowVisible(hwnd);
}


time_t Process::TimeNow()
{
	time_t rawTime;
	time(&rawTime);
	return rawTime;
}

struct tm Process::GetLocalTime()
{
	time_t rawTime = TimeNow();
	struct tm timeInfo;

	localtime_s(&timeInfo, &rawTime);

	return timeInfo;
}

double Process::GetTimeDifference(struct tm time1, struct tm time2)
{
	return difftime(mktime(&time1), mktime(&time2));
}


