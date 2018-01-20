//  DaemonMaster: RegManager.h
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

class RegManager
{

#define REG_PATH L"SYSTEM\\CurrentControlSet\\Services\\"
#define	PARAMS_SUBKEY L"\\Parameters"

public:
	static BOOL ReadParametersFromRegistry(const wstring& serviceName, ProcessStartInfo &daemonInfo);
	static wstring ReadString(HKEY hKey, const wstring &valueName);
	static DWORD ReadDWORD(HKEY hKey, const wstring &valueName);
	static BOOL ReadBool(HKEY hKey, const wstring &valueName);	
};
