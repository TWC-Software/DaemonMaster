#include "stdafx.h"
#include "Service.h"
#include "RegManager.h"

Service::Service(PWSTR serviceName, BOOL canStop, BOOL canShutdown, BOOL canPauseContinue) : CServiceBase(serviceName, canStop, canShutdown, canPauseContinue)
{
	m_fStopping = false;

	m_hStoppedEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
	if(m_hStoppedEvent == NULL)
	{
		throw GetLastError();
	}
}

Service::~Service()
{
	if(m_hStoppedEvent)
	{
		CloseHandle(m_hStoppedEvent);
		m_hStoppedEvent = NULL;
	}
}

void Service::OnStart(DWORD dwArgc, PWSTR * pszArgv)
{
	BOOL result = RegManager::ReadParametersFromRegistry(pszArgv[0], dm);
	
	if(!process)
		process = new Process(dm);

	process->Start();

	//WriteEventLogEntry(const_cast<wchar_t*>(pszArgv[0]), EVENTLOG_WARNING_TYPE);
	//HANDLE threadHandle = CreateThread(NULL, NULL, ServiceThread, this, NULL, NULL);
}

void Service::OnStop()
{
	m_fStopping = true;

	process->Stop();


	/*if(WaitForSingleObject(m_hStoppedEvent, INFINITE) != WAIT_OBJECT_0)
	{
		throw GetLastError();		
	}*/
}

DWORD WINAPI Service::ServiceThread(LPVOID params)
{
	return static_cast<Service*>(params)->ServiceWorkerThread();
}

DWORD WINAPI Service::ServiceWorkerThread()
{
	/*while (!m_fStopping)
	{

	}*/

	//Start stopping
	SetEvent(m_hStoppedEvent);
	return 0;
}

