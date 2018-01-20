//  DaemonMaster: DMService
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
#include "DMService.h"
#include "Service.h"


#define PLACEHOLDER_SERVICE_NAME L"DaemonMaster DevService"

int wmain(int argc, wchar_t *argv[], wchar_t *envp[])
{
	for(uint16_t i = 1; i < argc; i++)
	{
		if(_wcsicmp(argv[i], L"-console") == 0)
		{
			//Nothing
			return 0;
		}
		else if(_wcsicmp(argv[i], L"-service") == 0)
		{
			Service service(const_cast<wchar_t*>(PLACEHOLDER_SERVICE_NAME));
			if(!service.Run(service))
			{
				throw GetLastError();
			}
		}
		else if(_wcsicmp(argv[i], L"-killAllServices") == 0)
		{
			//TODO
			DMService::KillAllServices();
		}
		else if (_wcsicmp(argv[i], L"-deleteAllServices") == 0)
		{
			//TODO
		}
	}

	return 0;
}

void DMService::KillAllServices()
{
	
}

