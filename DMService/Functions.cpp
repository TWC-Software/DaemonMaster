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