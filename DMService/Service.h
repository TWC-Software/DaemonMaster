#pragma once
#include "CServiceBase.h"
#include "Process.h"

class Service : public CServiceBase
{
public:

	Service(PWSTR serviceName,
		BOOL canStop = TRUE,
		BOOL canShutdown = TRUE,
		BOOL canPauseContinue = FALSE);
	virtual ~Service();

protected:

	virtual void OnStart(DWORD dwArgc, PWSTR *pszArgv);
	virtual void OnStop();

	static DWORD WINAPI ServiceThread(LPVOID params);
	DWORD WINAPI ServiceWorkerThread();

private:
	BOOL m_fStopping;
	HANDLE m_hStoppedEvent;

	ProcessStartInfo dm;
	Process* process = NULL;
};

