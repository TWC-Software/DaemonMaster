//  DaemonMaster: Process.h
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
#pragma once
#include "ProcessStartInfo.h"

class Process
{
private:
	ProcessStartInfo pInfo;
	HANDLE autoKillJobHandle = NULL;
	HANDLE processHandle = NULL;
	HANDLE threadHandle= NULL;
	HANDLE waitHandle = NULL;
	DWORD processId = 0;
	DWORD restarts = 0;
	bool startInUserSession = false;

	time_t lastRestart;

	bool StartWithCreateProcess();
	bool StartOnActivUserSession();
	void CleanUp();
	void StopWatchingForExit();
	void StartWatchingForExit();
	bool AssignAutoKillJob();
	bool IsRunning() const;

	static time_t TimeNow();
	static struct tm GetLocalTime();
	static double GetTimeDifference(struct tm time1, struct tm time2);

	static void CALLBACK OnExitedCallback(PVOID params, BOOLEAN timerOrWaitFired);
	void OnExited();

	bool Kill();


	HWND GetMainWindowHandle() const;
	static BOOL CALLBACK EnumWindowsCallback(HWND hwnd, LPARAM lParam);
	static BOOL IsMainWindow(HWND hwnd);

public:
	Process();
	~Process();

	bool Start();
	bool Stop();
	void SetStartMode(bool startInUserSession);
	void SetProcessStartInfo(const ProcessStartInfo& psi);
	//void Pause();
	//void Resume();
};

