//  DaemonMasterService: Exceptions
//  
//  This file is part of DeamonMasterService.
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
//   along with DeamonMasterService.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "Exceptions.h"

using namespace std;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                              APICallFail                                                        //
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

APICallFail::APICallFail(const string& message, const DWORD errorCode) : runtime_error(message)
{
	this->errorCode = errorCode;
}

DWORD APICallFail::ErrorCode() const
{
	return errorCode;
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                  RegistryError  (based on runtime_error)                                        //
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

RegistryError::RegistryError(const string& message, const LSTATUS errorCode) : runtime_error(message)
{
	this->errorCode = errorCode;
}

LSTATUS RegistryError::ErrorCode() const
{
	return errorCode;
};
