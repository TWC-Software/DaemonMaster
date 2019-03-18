using System;
using System.Diagnostics;
using System.ServiceProcess;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMasterService;
using Microsoft.Deployment.WindowsInstaller;

namespace DaemonMaster.CustomActions
{
    public class CustomActions
    {
        //[CustomAction]
        //public static ActionResult KillAllServices(Session session)
        //{
        //    try
        //    {
        //        foreach (DmServiceDefinition service in RegistryManagement.LoadInstalledServices())
        //        {
        //            using (var serviceController = new ServiceController(service.ServiceName))
        //            {
        //                if (serviceController.Status == ServiceControllerStatus.Stopped)
        //                    continue;

        //                try
        //                {
        //                    serviceController.ExecuteCommand((int)ServiceCommands.ServiceKillProcessAndStop); //Kill command for the process
        //                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(2));
        //                }
        //                catch (System.ServiceProcess.TimeoutException)
        //                {
        //                    uint id = DaemonMasterUtils.GetProcessIdByServiceName(service.ServiceName);
        //                    Process process = Process.GetProcessById((int)id);
        //                    process.Kill();
        //                }
        //            }
        //        }

        //        return ActionResult.Success;
        //    }
        //    catch (Exception e)
        //    {
        //        return ActionResult.Failure;
        //    }
        //}

        [CustomAction]
        public static ActionResult DeleteAllServices(Session session)
        {
            session.Log("Beginning the uninstall of all services.");

            try
            {
                using (ServiceControlManager controlManager = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.AllAccess))
                {
                    foreach (DmServiceDefinition serviceItem in RegistryManagement.LoadInstalledServices())
                    {
                        using (ServiceHandle service = controlManager.OpenService(serviceItem.ServiceName, Advapi32.ServiceAccessRights.AllAccess))
                        {
                            Advapi32.ServiceStatusProcess serviceStatus = service.QueryServiceStatus();

                            using (var serviceController = new ServiceController(serviceItem.ServiceName))
                            {
                                if (serviceController.Status == ServiceControllerStatus.Stopped)
                                    continue;

                                try
                                {
                                    session.Log("Try killing " + serviceItem.ServiceName + " service.");
                                    serviceController.ExecuteCommand((int)ServiceCommands.ServiceKillProcessAndStop); //Kill command for the process
                                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                                }
                                catch (System.ServiceProcess.TimeoutException)
                                {
                                    session.Log("Terminate " + serviceItem.ServiceName + " service.");
                                    Process process = Process.GetProcessById((int)serviceStatus.processId);
                                    process.Kill();
                                }
                            }

                            service.DeleteService();
                            session.Log("Deleted " + serviceItem.ServiceName + " service successful.");
                        }
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
    }
}
