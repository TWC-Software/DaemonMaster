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