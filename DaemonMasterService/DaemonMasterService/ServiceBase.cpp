/****************************** Module Header ******************************\ 
* Module Name:  ServiceBase.cpp 
* Project:      CppWindowsService 
* Copyright (c) Microsoft Corporation. 
* Modified by TWC-Software, 15.7.2018
*  
* Provides a base class for a service that will exist as part of a service  
* application. ServiceBase must be derived from when creating a new service  
* class. 
*  
* This source is subject to the Microsoft Public License. 
* See http://www.microsoft.com/en-us/openness/resources/licenses.aspx#MPL. 
* All other rights reserved. 
*  
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,  
* EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED  
* WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
\***************************************************************************/ 
 
#include "stdafx.h"
#include "ServiceBase.h"


using namespace std;
 
#pragma region Static Members 
 
// Initialize the singleton service instance. 
ServiceBase *ServiceBase::_service = NULL;
 
 
// 
//   FUNCTION: ServiceBase::Run(ServiceBase &) 
// 
//   PURPOSE: Register the executable for a service with the Service Control  
//   Manager (SCM). After you call Run(ServiceBase), the SCM issues a Start  
//   command, which results in a call to the OnStart method in the service.  
//   This method blocks until the service has stopped. 
// 
//   PARAMETERS: 
//   * service - the reference to a ServiceBase object. It will become the  
//     singleton service instance of this service application. 
// 
//   RETURN VALUE: If the function succeeds, the return value is TRUE. If the  
//   function fails, the return value is FALSE. To get extended error  
//   information, call GetLastError. 
// 
bool ServiceBase::Run(ServiceBase &service) 
{
	_service = &service;
 
    SERVICE_TABLE_ENTRY serviceTable[] =  
    { 
        { service._serviceName, ServiceMain },
        { NULL, NULL } 
    }; 
 
    // Connects the main thread of a service process to the service control  
    // manager, which causes the thread to be the service control dispatcher  
    // thread for the calling process. This call returns when the service has  
    // stopped. The process should simply terminate when the call returns. 
    return StartServiceCtrlDispatcher(serviceTable); 
} 
 
 
// 
//   FUNCTION: ServiceBase::ServiceMain(DWORD, PWSTR *) 
// 
//   PURPOSE: Entry point for the service. It registers the handler function  
//   for the service and starts the service. 
// 
//   PARAMETERS: 
//   * dwArgc   - number of command line arguments 
//   * lpszArgv - array of command line arguments 
// 
void WINAPI ServiceBase::ServiceMain(DWORD dwArgc, PWSTR *pszArgv) 
{ 
	if(!_service)
	{
		Logger::Critical("_service in ServiceMain is NULL");
		return;
	}

    // Register the handler function for the service 
	_service->_serviceStatusHandle = RegisterServiceCtrlHandler(_service->_serviceName, ServiceCtrlHandler);

    if (!_service->_serviceStatusHandle)
    {
	    const DWORD lastError = GetLastError();
		// Log the error.   
		Logger::Critical("_serviceStatusHandle in ServiceMain is NULL" + lastError);
		return;
    } 
 
    // Start the service. 
	_service->Start(dwArgc, pszArgv);
} 
 
 
// 
//   FUNCTION: ServiceBase::ServiceCtrlHandler(DWORD) 
// 
//   PURPOSE: The function is called by the SCM whenever a control code is  
//   sent to the service.  
// 
//   PARAMETERS: 
//   * dwCtrlCode - the control code. This parameter can be one of the  
//   following values:  
// 
//     SERVICE_CONTROL_CONTINUE 
//     SERVICE_CONTROL_INTERROGATE 
//     SERVICE_CONTROL_NETBINDADD 
//     SERVICE_CONTROL_NETBINDDISABLE 
//     SERVICE_CONTROL_NETBINDREMOVE 
//     SERVICE_CONTROL_PARAMCHANGE 
//     SERVICE_CONTROL_PAUSE 
//     SERVICE_CONTROL_SHUTDOWN 
//     SERVICE_CONTROL_STOP 
// 
//   This parameter can also be a user-defined control code ranges from 128  
//   to 255. 
// 
void WINAPI ServiceBase::ServiceCtrlHandler(DWORD dwCtrl) 
{ 
    switch (dwCtrl) 
    { 
    case SERVICE_CONTROL_STOP: _service->Stop(); break; 
    case SERVICE_CONTROL_PAUSE: _service->Pause(); break;
    case SERVICE_CONTROL_CONTINUE: _service->Continue(); break;
    case SERVICE_CONTROL_SHUTDOWN: _service->Shutdown(); break;
    case SERVICE_CONTROL_INTERROGATE: break; 
    default: break; 
    } 
} 
 
#pragma endregion 
 
 
#pragma region Service Constructor and Destructor 
 
// 
//   FUNCTION: ServiceBase::ServiceBase(PWSTR, BOOL, BOOL, BOOL) 
// 
//   PURPOSE: The constructor of ServiceBase. It initializes a new instance  
//   of the ServiceBase class. The optional parameters (fCanStop,  
///  fCanShutdown and fCanPauseContinue) allow you to specify whether the  
//   service can be stopped, paused and continued, or be notified when system  
//   shutdown occurs. 
// 
//   PARAMETERS: 
//   * pszServiceName - the name of the service 
//   * fCanStop - the service can be stopped 
//   * fCanShutdown - the service is notified when system shutdown occurs 
//   * fCanPauseContinue - the service can be paused and continued 
// 
ServiceBase::ServiceBase(wstring& serviceName, bool canStop, bool canShutdown, bool canPauseContinue) 
{  
	//Convert wstring to PWSTR and save it in _serviceName
	_serviceName = &serviceName[0];


    _serviceStatusHandle = NULL; 
 
    // The service runs in its own process. 
    _serviceStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS; 
 
    // The service is starting. 
    _serviceStatus.dwCurrentState = SERVICE_START_PENDING; 
 
    // The accepted commands of the service. 
    DWORD dwControlsAccepted = 0; 
    if (canStop)  
        dwControlsAccepted |= SERVICE_ACCEPT_STOP; 
    if (canShutdown)  
        dwControlsAccepted |= SERVICE_ACCEPT_SHUTDOWN; 
    if (canPauseContinue)  
        dwControlsAccepted |= SERVICE_ACCEPT_PAUSE_CONTINUE; 
    _serviceStatus.dwControlsAccepted = dwControlsAccepted; 
 
    _serviceStatus.dwWin32ExitCode = NO_ERROR; 
    _serviceStatus.dwServiceSpecificExitCode = 0; 
    _serviceStatus.dwCheckPoint = 0; 
    _serviceStatus.dwWaitHint = 0; 
} 
 
 
// 
//   FUNCTION: ServiceBase::~ServiceBase() 
// 
//   PURPOSE: The virtual destructor of ServiceBase.  
// 
ServiceBase::~ServiceBase(void) 
{ 
} 
 
#pragma endregion 
 
 
#pragma region Service Start, Stop, Pause, Continue, and Shutdown 
 
// 
//   FUNCTION: ServiceBase::Start(DWORD, PWSTR *) 
// 
//   PURPOSE: The function starts the service. It calls the OnStart virtual  
//   function in which you can specify the actions to take when the service  
//   starts. If an error occurs during the startup, the error will be logged  
//   in the Application event log, and the service will be stopped. 
// 
//   PARAMETERS: 
//   * dwArgc   - number of command line arguments 
//   * lpszArgv - array of command line arguments 
// 
void ServiceBase::Start(DWORD argc, PWSTR *argv) 
{ 
    try 
    { 
        // Tell SCM that the service is starting. 
        SetServiceStatus(SERVICE_START_PENDING); 
 
        // Perform service-specific initialization. 
        OnStart(argc, argv);
 
        // Tell SCM that the service is started. 
        SetServiceStatus(SERVICE_RUNNING); 
    } 
    catch (const std::exception& ex)
    { 
		// Log the error.   
		Logger::Error("Service failed to start: " + string(ex.what()));

        // Set the service status to be stopped. 
        SetServiceStatus(SERVICE_STOPPED);
    } 
    catch (...) 
    { 
        // Log the error. 
		Logger::Error("Service failed to start: " + to_string(EVENTLOG_ERROR_TYPE));
 
        // Set the service status to be stopped. 
        SetServiceStatus(SERVICE_STOPPED); 
    } 
} 
 
 
// 
//   FUNCTION: ServiceBase::OnStart(DWORD, PWSTR *) 
// 
//   PURPOSE: When implemented in a derived class, executes when a Start  
//   command is sent to the service by the SCM or when the operating system  
//   starts (for a service that starts automatically). Specifies actions to  
//   take when the service starts. Be sure to periodically call  
//   ServiceBase::SetServiceStatus() with SERVICE_START_PENDING if the  
//   procedure is going to take long time. You may also consider spawning a  
//   new thread in OnStart to perform time-consuming initialization tasks. 
// 
//   PARAMETERS: 
//   * dwArgc   - number of command line arguments 
//   * lpszArgv - array of command line arguments 
// 
void ServiceBase::OnStart(DWORD argc, PWSTR *argv) 
{ 
} 
 
 
// 
//   FUNCTION: ServiceBase::Stop() 
// 
//   PURPOSE: The function stops the service. It calls the OnStop virtual  
//   function in which you can specify the actions to take when the service  
//   stops. If an error occurs, the error will be logged in the Application  
//   event log, and the service will be restored to the original state. 
// 
void ServiceBase::Stop() 
{ 
    DWORD originalState = _serviceStatus.dwCurrentState; 
    try 
    { 
        // Tell SCM that the service is stopping. 
        SetServiceStatus(SERVICE_STOP_PENDING); 
 
        // Perform service-specific stop operations. 
        OnStop(); 
 
        // Tell SCM that the service is stopped. 
        SetServiceStatus(SERVICE_STOPPED); 
    } 
    catch (const exception& ex) 
    { 
        // Log the error. 
		Logger::Error("Service failed to stop: " + string(ex.what()));
 
        // Set the orginal service status. 
        SetServiceStatus(originalState); 
    } 
    catch (...) 
    { 
        // Log the error. 
		Logger::Error("Service failed to stop");
 
        // Set the orginal service status. 
        SetServiceStatus(originalState); 
    } 
} 
 
 
// 
//   FUNCTION: ServiceBase::OnStop() 
// 
//   PURPOSE: When implemented in a derived class, executes when a Stop  
//   command is sent to the service by the SCM. Specifies actions to take  
//   when a service stops running. Be sure to periodically call  
//   ServiceBase::SetServiceStatus() with SERVICE_STOP_PENDING if the  
//   procedure is going to take long time.  
// 
void ServiceBase::OnStop() 
{ 
} 
 
 
// 
//   FUNCTION: ServiceBase::Pause() 
// 
//   PURPOSE: The function pauses the service if the service supports pause  
//   and continue. It calls the OnPause virtual function in which you can  
//   specify the actions to take when the service pauses. If an error occurs,  
//   the error will be logged in the Application event log, and the service  
//   will become running. 
// 
void ServiceBase::Pause() 
{ 
    try 
    { 
        // Tell SCM that the service is pausing. 
        SetServiceStatus(SERVICE_PAUSE_PENDING); 
 
        // Perform service-specific pause operations. 
        OnPause(); 
 
        // Tell SCM that the service is paused. 
        SetServiceStatus(SERVICE_PAUSED); 
    } 
    catch (const exception& ex) 
    { 
        // Log the error. 
		Logger::Error("Service failed to pause: " + string(ex.what()));
 
        // Tell SCM that the service is still running. 
        SetServiceStatus(SERVICE_RUNNING); 
    } 
    catch (...) 
    { 
        // Log the error. 
		Logger::Error("Service failed to pause");
 
        // Tell SCM that the service is still running. 
        SetServiceStatus(SERVICE_RUNNING); 
    } 
} 
 
 
// 
//   FUNCTION: ServiceBase::OnPause() 
// 
//   PURPOSE: When implemented in a derived class, executes when a Pause  
//   command is sent to the service by the SCM. Specifies actions to take  
//   when a service pauses. 
// 
void ServiceBase::OnPause() 
{ 
} 
 
 
// 
//   FUNCTION: ServiceBase::Continue() 
// 
//   PURPOSE: The function resumes normal functioning after being paused if 
//   the service supports pause and continue. It calls the OnContinue virtual  
//   function in which you can specify the actions to take when the service  
//   continues. If an error occurs, the error will be logged in the  
//   Application event log, and the service will still be paused. 
// 
void ServiceBase::Continue() 
{ 
    try 
    { 
        // Tell SCM that the service is resuming. 
        SetServiceStatus(SERVICE_CONTINUE_PENDING); 
 
        // Perform service-specific continue operations. 
        OnContinue(); 
 
        // Tell SCM that the service is running. 
        SetServiceStatus(SERVICE_RUNNING); 
    } 
    catch (const exception& ex) 
    { 
        // Log the error. 
		Logger::Error("Service failed to continue: " + string(ex.what()));
 
        // Tell SCM that the service is still paused. 
        SetServiceStatus(SERVICE_PAUSED); 
    } 
    catch (...) 
    { 
        // Log the error. 
		Logger::Error("Service failed to shutdown" + to_string(EVENTLOG_ERROR_TYPE));
 
        // Tell SCM that the service is still paused. 
        SetServiceStatus(SERVICE_PAUSED); 
    } 
} 
 
 
// 
//   FUNCTION: ServiceBase::OnContinue() 
// 
//   PURPOSE: When implemented in a derived class, OnContinue runs when a  
//   Continue command is sent to the service by the SCM. Specifies actions to  
//   take when a service resumes normal functioning after being paused. 
// 
void ServiceBase::OnContinue() 
{ 
} 
 
 
// 
//   FUNCTION: ServiceBase::Shutdown() 
// 
//   PURPOSE: The function executes when the system is shutting down. It  
//   calls the OnShutdown virtual function in which you can specify what  
//   should occur immediately prior to the system shutting down. If an error  
//   occurs, the error will be logged in the Application event log. 
// 
void ServiceBase::Shutdown() 
{ 
    try 
    { 
        // Perform service-specific shutdown operations. 
        OnShutdown(); 
 
        // Tell SCM that the service is stopped. 
        SetServiceStatus(SERVICE_STOPPED); 
    } 
    catch (const exception& ex) 
    { 
        // Log the error. 
		Logger::Error("Service failed to shutdown: " + string(ex.what()));
    } 
    catch (...) 
    { 
        // Log the error. 
		Logger::Error("Service failed to shutdown");
    } 
} 
 
 
// 
//   FUNCTION: ServiceBase::OnShutdown() 
// 
//   PURPOSE: When implemented in a derived class, executes when the system  
//   is shutting down. Specifies what should occur immediately prior to the  
//   system shutting down. 
// 
void ServiceBase::OnShutdown() 
{ 
} 
 
#pragma endregion 
 
 
#pragma region Helper Functions 
 
// 
//   FUNCTION: ServiceBase::SetServiceStatus(DWORD, DWORD, DWORD) 
// 
//   PURPOSE: The function sets the service status and reports the status to  
//   the SCM. 
// 
//   PARAMETERS: 
//   * dwCurrentState - the state of the service 
//   * dwWin32ExitCode - error code to report 
//   * dwWaitHint - estimated time for pending operation, in milliseconds 
// 
void ServiceBase::SetServiceStatus(DWORD currentState, DWORD win32ExitCode, DWORD waitHint) 
{ 
    static DWORD dwCheckPoint = 1; 
 
    // Fill in the SERVICE_STATUS structure of the service. 
 
    _serviceStatus.dwCurrentState = currentState;
    _serviceStatus.dwWin32ExitCode = win32ExitCode;
    _serviceStatus.dwWaitHint = waitHint;
 
    _serviceStatus.dwCheckPoint =  
        ((currentState == SERVICE_RUNNING) ||
        (currentState == SERVICE_STOPPED)) ?
        0 : dwCheckPoint++; 
 
    // Report the status of the service to the SCM. 
    ::SetServiceStatus(_serviceStatusHandle, &_serviceStatus); 
} 

#pragma endregion