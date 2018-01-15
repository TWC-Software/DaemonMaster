#include "stdafx.h"
#include "ProcessManager.h"


ProcessManager::ProcessManager(const ProcessStartInfo& pInfo)
{
	proc = new Process(pInfo);
}

ProcessManager::~ProcessManager()
{
}

bool ProcessManager::Start()
{
	return proc->Start();
}

bool ProcessManager::Stop()
{
	return proc->Stop();
}

void ProcessManager::OnExitedEvent()
{
	//proc->Start();
}


