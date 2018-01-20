//  DaemonMaster: Service
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
#include "Service.h"
#include "RegManager.h"

Service::Service(PWSTR serviceName, BOOL canStop, BOOL canShutdown, BOOL canPauseContinue) : CServiceBase(serviceName, canStop, canShutdown, canPauseContinue)
{
}

void Service::OnStart(DWORD dwArgc, PWSTR * pszArgv)
{
	bool result = RegManager::ReadParametersFromRegistry(pszArgv[0], psi);

	process.SetProcessStartInfo(psi);

	for(uint16_t i = 1; i < dwArgc; i++)
	{
		if(_wcsicmp(pszArgv[i], L"-startInUserSession") == 0)
		{
			process.SetStartMode(true);
		}
	}

	if(!process.Start())
	{
		Stop();
	}

	//WriteEventLogEntry(const_cast<wchar_t*>(pszArgv[0]), EVENTLOG_WARNING_TYPE);
}

void Service::OnStop()
{
	process.Stop();
}
