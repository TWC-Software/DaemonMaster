#pragma once
#include "ProcessStartInfo.h"

class Process
{
private:
	ProcessStartInfo pInfo;
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

	static time_t TimeNow();
	static struct tm GetLocalTime();
	static double GetTimeDifference(struct tm time1, struct tm time2);

	static void CALLBACK OnExitedCallback(PVOID params, BOOLEAN timerOrWaitFired);
	void OnExited();

	bool Kill();


	HWND GetMainWindowHandle();
	static BOOL CALLBACK EnumWindowsCallback(HWND hwnd, LPARAM lParam);
	static BOOL IsMainWindow(HWND hwnd);

public:
	Process(const ProcessStartInfo& dm);
	~Process();

	bool Start();
	bool Stop();
	void SetStartMode(bool startInUserSession);
	//void Pause();
	//void Resume();
};

