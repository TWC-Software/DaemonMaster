//  DaemonMaster: Service.h
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
#include "CServiceBase.h"
#include "Process.h"

class Service : public CServiceBase
{
public:

	Service(PWSTR serviceName,
		BOOL canStop = TRUE,
		BOOL canShutdown = TRUE,
		BOOL canPauseContinue = FALSE);
	 virtual ~Service() = default;

protected:

	virtual void OnStart(DWORD dwArgc, PWSTR *pszArgv);
	virtual void OnStop();

private:
	ProcessStartInfo psi;
	Process process;
};

