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

#pragma once


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                              APICallFail                                                        //
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class APICallFail : public std::runtime_error
{
public:
	APICallFail(const std::string& message, const DWORD errorCode);
	DWORD ErrorCode() const;

private:
	DWORD errorCode;
};


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                  RegistryError  (based on runtime_error)                                        //
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class RegistryError : public std::runtime_error
{
public:
	RegistryError(const std::string& message, const LSTATUS status);
	LSTATUS ErrorCode() const;

private:
	LSTATUS errorCode;
};