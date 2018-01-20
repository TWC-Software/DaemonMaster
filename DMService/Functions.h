//  DaemonMaster: Functions.h
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

class Functions
{
public:
	static inline void TrimBegin(wstring &s);
	static inline void TrimEnd(wstring &s);
	static inline void Trim(wstring &s);

	static wstring BuildCommandLineArgs(wstring filePath, wstring args);
	static wstring CombinePaths(wstring filePath, const wstring& fileName);

	static bool EnablePrivilege(LPCWSTR privilege);
	static bool DisablePrivilege(LPCWSTR privilege);
	static bool SetPrivilege(HANDLE hToken, LPCWSTR lpszPrivilege, BOOL);

	static void KillAllServices();
	static void KillAllChildProcesses(DWORD parentPid);
	static void KillProcessWithId(DWORD pid);
};

