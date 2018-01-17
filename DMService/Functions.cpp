#include "stdafx.h"
#include "Functions.h"
#include <filesystem>

inline void Functions::TrimBegin(wstring &s)
{
	s.erase(s.begin(), find_if(s.begin(), s.end(), [](int ch) { return !isspace(ch); }));
}

inline void Functions::TrimEnd(wstring &s)
{
	s.erase(find_if(s.rbegin(), s.rend(), [](int ch) {return !isspace(ch); }).base(), s.end());
}

inline void Functions::Trim(wstring &s) {
	TrimBegin(s);
	TrimEnd(s);
}

wstring Functions::BuildCommandLineArgs(wstring filePath, wstring args)
{
	Trim(filePath);
	Trim(args);

	//Quotate the filePath
	wstring result = L"\"" + filePath + L"\"";
	
	if (!args.empty())
	{
		result += L" " + args;
	}

	return result;
}

wstring Functions::CombinePaths(wstring filePath, const wstring& fileName)
{
	filePath.insert(filePath.length() - 1, 2, '\\');
	filePath.insert(filePath.length() - 1, fileName);	

	return filePath;
}

bool Functions::EnablePrivilege(LPCWSTR privilege)
{
	HANDLE hToken = NULL;
	if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, &hToken))
		return false;

	bool result = SetPrivilege(hToken, privilege, true);

	CloseHandle(hToken);
	return result;
}

bool Functions::DisablePrivilege(LPCWSTR privilege)
{
	HANDLE hToken = NULL;
	if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, &hToken))
		return false;

	bool result = SetPrivilege(hToken, privilege, false);

	CloseHandle(hToken);
	return result;
}

//https://msdn.microsoft.com/en-us/library/aa446619.aspx
bool Functions::SetPrivilege(
	HANDLE hToken,          // access token handle
	LPCWSTR lpszPrivilege,  // name of privilege to enable/disable
	BOOL bEnablePrivilege   // to enable or disable privilege
)
{
	TOKEN_PRIVILEGES tp;
	LUID luid;

	if (!LookupPrivilegeValueW(
		NULL,            // lookup privilege on local system
		lpszPrivilege,   // privilege to lookup 
		&luid))        // receives LUID of privilege
	{
		//TODO: Log
		//printf("LookupPrivilegeValue error: %u\n", GetLastError());
		return FALSE;
	}

	tp.PrivilegeCount = 1;
	tp.Privileges[0].Luid = luid;
	if (bEnablePrivilege)
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
	else
		tp.Privileges[0].Attributes = 0;

	// Enable the privilege or disable all privileges.

	if (!AdjustTokenPrivileges(
		hToken,
		FALSE,
		&tp,
		sizeof(TOKEN_PRIVILEGES),
		static_cast<PTOKEN_PRIVILEGES>(NULL),
		static_cast<PDWORD>(NULL)))
	{
		//printf("AdjustTokenPrivileges error: %u\n", GetLastError());
		return FALSE;
	}

	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)

	{
		//printf("The token does not have the specified privilege. \n");
		return FALSE;
	}

	return TRUE;
}
