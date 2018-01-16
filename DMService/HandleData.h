#pragma once

struct HandleData
{
	HandleData()
	{
		pid = 0;
		bestHandle = NULL;
	}

	DWORD pid;
	HWND bestHandle;
};
