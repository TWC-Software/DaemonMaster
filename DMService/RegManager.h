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
