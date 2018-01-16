#pragma once

class Functions
{
public:
	static inline void TrimBegin(wstring &s);
	static inline void TrimEnd(wstring &s);
	static inline void Trim(wstring &s);

	static wstring BuildCommandLineArgs(wstring filePath, wstring args);
	static wstring CombinePaths(wstring filePath, const wstring& fileName);
};

