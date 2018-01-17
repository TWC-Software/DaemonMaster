#pragma once

class ProcessStartInfo
{
private: 
wstring sFileDir		= L"";
wstring sFileName		= L"";
wstring sFullPath		= L"";
wstring sParamaters		= L"";

bool bUseLocalSystemAccount = true;
bool bUnlimitedRestarts		= false;
DWORD dMaxRestarts			= 3;
DWORD dMaxRestartsResetTime = 0;

bool bIsConsoleApp	= false;
bool bUseCtrlC		= false;

public:
	/*ProcessStartInfo()
	{
		sFileDir = L"";
		sFileName = L"";
		sFullPath = L"";
		sParamaters = L"";
		
		bUseLocalSystemAccount = true;
		dMaxRestarts = 3;
		bIsConsoleApp = false;
		bUseCtrlC = false;
	}*/

	const wstring& GetFileDir() const { return sFileDir; }
	void SetFileDir(const wstring& filePath)
	{
		wstring tmp = filePath;
		
		for (uint16_t i = 0; i < tmp.length(); i++)
			if (tmp[i] == '\\')
			{
				tmp.insert(i, 1, '\\');
				i++; // Skip inserted char
			}

		sFileDir = tmp;
	}

	const wstring& GetFileName() const { return sFileName; }
	void SetFileName(const wstring& fileName) { sFileName = fileName; }

	const wstring& GetFullPath() const{ return sFullPath; }
	void SetFullPath(const wstring& fullPath) { sFullPath = fullPath; }

	const wstring& GetParameters() const { return sParamaters; }
	void SetParameters(const wstring& params) { sParamaters = params; }


	bool GetUnlimitedRestarts() const { return bUnlimitedRestarts; }
	void SetUnlimitedRestarts(bool unlimitedRestarts) { bUnlimitedRestarts = unlimitedRestarts; }

	const DWORD& GetMaxRestarts() const { return dMaxRestarts; }
	void SetMaxRestarts(const DWORD& maxRestarts) { dMaxRestarts = maxRestarts; }

	const DWORD& GetMaxRestartsResetTime() const { return dMaxRestartsResetTime; }
	void SetMaxRestartsResetTime(const DWORD& maxRestartsResetTime) { dMaxRestartsResetTime = maxRestartsResetTime; }


	bool GetIsConsoleApp() const { return bIsConsoleApp; }
	void SetIsConsoleApp(bool isConsoleApp) { bIsConsoleApp = isConsoleApp; }

	bool GetUseCtrlC() const { return bUseCtrlC; }
	void SetUseCtrlC(bool useCtrlC) { bUseCtrlC = useCtrlC; }




};

