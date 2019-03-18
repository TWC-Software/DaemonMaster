using System;
using System.Diagnostics;
using System.ServiceProcess;
using DaemonMaster.Core.Jobs;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMasterService;

namespace DaemonMaster.Core
{
    public class CustomActionsInstaller
    {
        public static void KillAllServices()
        {
            foreach (DmServiceDefinition service in RegistryManagement.LoadInstalledServices())
            {
                using (var serviceController = new ServiceController(service.ServiceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                        continue;

                    try
                    {
                        serviceController.ExecuteCommand((int)ServiceCommands.ServiceKillProcessAndStop); //Kill command for the process
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(2));
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        if (KillChildProcessJob.IsSupportedWindowsVersion)
                        {
                            uint id = DaemonMasterUtils.GetProcessIdByServiceName(service.ServiceName);
                            Process process = Process.GetProcessById((int)id);
                            process.Kill();
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        public static void DeleteAllServices()
        {
            KillAllServices();

            using (ServiceControlManager serviceController = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.AllAccess))
            {
                foreach (DmServiceDefinition serviceItem in RegistryManagement.LoadInstalledServices())
                {
                    using (ServiceHandle service = serviceController.OpenService(serviceItem.ServiceName, Advapi32.ServiceAccessRights.AllAccess))
                    {
                        Advapi32.ServiceStatusProcess status = service.QueryServiceStatus();
                        if (status.currentState != Advapi32.ServiceCurrentState.Stopped)
                            continue;

                        service.DeleteService();
                    }
                }
            }
        }
    }
}
