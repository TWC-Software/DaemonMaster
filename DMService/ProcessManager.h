#pragma once
#include "Process.h"

class ProcessManager
{
private:
	Process* proc = NULL;

	void OnExitedEvent();


public:
	ProcessManager(const ProcessStartInfo& pInfo);
	~ProcessManager();
	bool Start();
	bool Stop();
};

