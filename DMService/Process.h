#pragma once
class Process
{
private:
	CRITICAL_SECTION criticalSection;

	ProcessStartInfo pInfo;
	HANDLE processHandle;
	HANDLE threadHandle;
	HANDLE waitHandle;
	DWORD processId;

	bool StartWithCreateProcess();
	void CleanUp();
	void StopWatchingForExit();
	void StartWatchingForExit();

	static void CALLBACK OnExitedCallback(PVOID params, BOOLEAN timerOrWaitFired);
	void OnExited();

public:
	Process(const ProcessStartInfo& dm);
	~Process();

	bool Start();
	bool Stop();
	//void Pause();
	//void Resume();

};

