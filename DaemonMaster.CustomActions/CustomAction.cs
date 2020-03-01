using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMasterService;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace DaemonMaster.CustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult DeleteAllServices(Session session)
        {
            string appFolder = session.CustomActionData["APPDIR"];
            if (string.IsNullOrWhiteSpace(appFolder) || !Directory.Exists(appFolder))
            {
                session.Log("AppFolder is null or invalid.");
                return ActionResult.Failure;
            }

            session.Log("Beginning the uninstall of all services.");

            try
            {
                using (ServiceControlManager controlManager = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    //Get all services for this installation
                    List<string> serviceNameList = new List<string>();
                    using (RegistryKey mainKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\", RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        foreach (string serviceName in mainKey.GetSubKeyNames())
                        {
                            using (RegistryKey key = mainKey.OpenSubKey(serviceName, RegistryKeyPermissionCheck.ReadSubTree))
                            {
                                //If the key invalid, skip this service
                                if (key == null)
                                    continue;

                                //Get the exe path of the service to determine later if its a service from DaemonMaster
                                string serviceExePath = Convert.ToString(key.GetValue("ImagePath") ?? string.Empty);

                                //If the serviceExePath is invalid skip this service
                                if (string.IsNullOrWhiteSpace(serviceExePath))
                                    continue;

                                if (!serviceExePath.Contains(appFolder + ServiceControlManager.DmServiceFileName)) //Not possible to use ServiceControlManager.DmServiceExe because the AppDomain changed here => so that we need to rebuilt the path.
                                    continue;

                                serviceNameList.Add(serviceName);
                            }
                        }
                    }

                    foreach (string serviceName in serviceNameList)
                    {
                        using (ServiceHandle service = controlManager.OpenService(serviceName, Advapi32.ServiceAccessRights.AllAccess))
                        {
                            session.Log("Try to delete " + serviceName);

                            try
                            {
                                Advapi32.ServiceStatusProcess serviceStatus = service.QueryServiceStatus();

                                if (serviceStatus.currentState != Advapi32.ServiceCurrentState.Stopped)
                                {
                                    session.Log("Try killing " + serviceName + " service.");
                                    service.ExecuteCommand((int)ServiceCommands.ServiceKillProcessAndStop);

                                    try
                                    {
                                        service.WaitForStatus(Advapi32.ServiceCurrentState.Stopped, TimeSpan.FromSeconds(5));
                                    }
                                    catch (TimeoutException)
                                    {
                                        session.Log("Terminate " + serviceName + " service.");
                                        Process process = Process.GetProcessById((int)serviceStatus.processId);
                                        process.Kill();
                                    }
                                }

                                service.DeleteService();
                                session.Log("Deleted " + serviceName + " service successful.");
                            }
                            catch (Exception e)
                            {
                                session.Log("Deletion of " + serviceName + " failed.\n" + e.Message + "\n" + e.StackTrace);
                                //continue
                            }
                        }
                    }
                }

                session.Log("Uninstall of all services was successful.");
                return ActionResult.Success;
            }
            catch (Exception e)
            {
                session.Log(e.Message + "\n" + e.StackTrace);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult RemoveResidualFiles(Session session)
        {
            try
            {
                session.Log("Beginning the uninstall of residual files.");

                string appFolder = session.CustomActionData["APPDIR"];

                DirectoryInfo di = new DirectoryInfo(appFolder);
                if (!string.IsNullOrWhiteSpace(appFolder) && di.Exists)
                {
                    di.Delete(true);

                    //Delete parent folder when it is empty
                    di = di.Parent;
                    if (di != null && di.GetFiles().Length == 0 && di.GetDirectories().Length == 0)
                    {
                        di.Delete(true);
                    }
                }

                return ActionResult.Success;
            }
            catch (Exception e)
            {
                session.Log(e.Message + "\n" + e.StackTrace);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult RemoveEventLogs(Session session)
        {
            try
            {
                session.Log("Beginning the uninstall of the event log source.");

                EventLogManager.RemoveSource();

                session.Log("Uninstall of the event log source was successful.");
                return ActionResult.Success;
            }
            catch (Exception e)
            {
                session.Log(e.Message + "\n" + e.StackTrace);
                return ActionResult.Failure;
            }
        }
    }
}
