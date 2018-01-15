#pragma once
#include "stdafx.h"
#include <filesystem>

using namespace std;

class ProcessStartInfo
{
private: 
wstring sParamaters;
wstring sFileDir;
wstring sFileName;
wstring sFullPath;

bool useLocalSystemAccount;
int maxRestarts;

bool consoleApp;
bool useCtrlC;

public:
	ProcessStartInfo()
	{
		sParamaters = L"";
		sFileDir = L"";
		sFileName = L"";
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
};

