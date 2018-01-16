#pragma once

class ProcessStartInfo
{
private: 
wstring sFileDir;
wstring sFileName;
wstring sFullPath;
wstring sParamaters;

bool bUseLocalSystemAccount;
DWORD dMaxRestarts;
DWORD dMaxRestartsResetTime = 0;

bool bIsConsoleApp;
bool bUseCtrlC;

public:
	ProcessStartInfo()
	{
		sFileDir = L"";
		sFileName = L"";
		sFullPath = L"";
		sParamaters = L"";
		
		bUseLocalSystemAccount = true;
		dMaxRestarts = 3;
		bIsConsoleApp = false;
		bUseCtrlC = false;
	}

	wstring GetFileDir() const { return sFileDir; }
	void SetFileDir(wstring filePath)
	{
		wstring tmp = filePath;
		
		for (int i = 0; i < tmp.length(); i++)
			if (tmp[i] == '\\')
			{
				tmp.insert(i, 1, '\\');
				i++; // Skip inserted char
			}

		sFileDir = tmp;
	}

	wstring GetFileName() const { return sFileName; }
	void SetFileName(wstring fileName) { sFileName = fileName; }

	const wstring& GetFullPath() const{ return sFullPath; }
	void SetFullPath(wstring fullPath) { sFullPath = fullPath; }

	wstring GetParameters() const { return sParamaters; }
	void SetParameters(wstring params) { sParamaters = params; }

	bool GetIsConsoleApp() const { return bIsConsoleApp; }
	void SetIsConsoleApp(bool isConsoleApp) { bIsConsoleApp = isConsoleApp; }

	bool GetUseCtrlC() const { return bUseCtrlC; }
	void SetUseCtrlC(bool useCtrlC) { bUseCtrlC = useCtrlC; }

	DWORD GetMaxRestarts() const { return dMaxRestarts; }
	void SetMaxRestarts(DWORD maxRestarts) { dMaxRestarts = maxRestarts; }

	DWORD GetMaxRestartsResetTime() const { return dMaxRestartsResetTime; }
	void SetMaxRestartsResetTime(DWORD maxRestartsResetTime) { dMaxRestartsResetTime = maxRestartsResetTime; }
};

