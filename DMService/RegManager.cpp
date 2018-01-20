//  DaemonMaster: RegManager
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
#include "RegManager.h"
#include "Functions.h"

// https://msdn.microsoft.com/en-us/magazine/mt808504.aspx
BOOL RegManager::ReadParametersFromRegistry(const wstring& serviceName, ProcessStartInfo& daemonInfo)
{
	const wstring regPath = REG_PATH + serviceName + PARAMS_SUBKEY;

	HKEY hKey;
	LONG errorStatus = RegOpenKeyExW(HKEY_LOCAL_MACHINE, regPath.c_str(), 0, KEY_READ, &hKey);

	if (errorStatus != ERROR_SUCCESS)	
		return false;

	try
	{
		daemonInfo.SetFileDir(ReadString(hKey, L"FileDir"));
		daemonInfo.SetFileName(ReadString(hKey, L"FileName"));
		daemonInfo.SetFullPath(Functions::CombinePaths(daemonInfo.GetFileDir(), daemonInfo.GetFileName()));
		daemonInfo.SetParameters(ReadString(hKey, L"Parameter"));

		daemonInfo.SetIsConsoleApp(ReadBool(hKey, L"ConsoleApplication"));
		daemonInfo.SetUseCtrlC(ReadBool(hKey, L"UseCtrlC"));

		daemonInfo.SetMaxRestarts(ReadDWORD(hKey, L"MaxRestarts"));
		daemonInfo.SetMaxRestartsResetTime(ReadDWORD(hKey, L"CounterResetTime"));

		RegCloseKey(hKey);
		return true;
	}
	catch (...)
	{
		RegCloseKey(hKey);
		return false;
	}
}

wstring RegManager::ReadString(HKEY hKey, const wstring &valueName)
{
	LSTATUS retCode;
	DWORD dataSize = {};

	//Get data size
	retCode = RegGetValueW(hKey, NULL, valueName.c_str(), RRF_RT_REG_SZ, nullptr, nullptr, &dataSize);
	if(retCode != ERROR_SUCCESS)
	{
		throw GetLastError();
	}

	//Get the lenght in wchars
	DWORD stringLengthInWchars = dataSize / sizeof(wchar_t);

	wstring data;
	data.resize(stringLengthInWchars);

	//The &data[0] is the address of the wstring internal buffer that will be written by the RegGetValue API.
	retCode      = RegGetValueW(hKey, NULL, valueName.c_str(), RRF_RT_REG_SZ, nullptr, &data[0], &dataSize);
	if (retCode != ERROR_SUCCESS)
	{
		throw GetLastError();
	}

	//Remove double NUL-terminator at the end of the string
	stringLengthInWchars--;
	data.resize(stringLengthInWchars);

	return data;
}

DWORD RegManager::ReadDWORD(HKEY hKey, const wstring &valueName)
{
	LSTATUS retCode;
	DWORD dataSize = sizeof(DWORD);
	DWORD data;

	retCode = RegGetValueW(hKey, NULL, valueName.c_str(), RRF_RT_DWORD, NULL, &data, &dataSize);
	if (retCode != ERROR_SUCCESS)
	{
		throw GetLastError();
	}

	return data;
}

BOOL RegManager::ReadBool(HKEY hKey, const wstring &valueName)
{
	DWORD const result = ReadDWORD(hKey, valueName);

	if(result != 0)
	{
		return true;
	}

	return false;
}