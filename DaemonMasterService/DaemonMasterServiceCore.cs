using DaemonMasterService.Win32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterService.Core
{
    public static class DaemonMasterServiceCore
    {

        public static bool CloseConsoleApplication(bool useCtrlC, uint Id)
        {
            if (useCtrlC)
            {
                return KERNEL32.GenerateConsoleCtrlEvent((uint)KERNEL32.CtrlEvent.CTRL_C_EVENT, Id);
            }
            else
            {
                return KERNEL32.GenerateConsoleCtrlEvent((uint)KERNEL32.CtrlEvent.CTRL_BREAK_EVENT, Id);

            }
        }

        public static int PauseProcess(uint Id)
        {
            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    bool value = KERNEL32.SuspendThread(processHandle);

                    if (value)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                finally
                {
                    KERNEL32.CloseHandle(processHandle);
                }
            }

            return -1;
        }

        public static int ResumeProcess(uint Id)
        {
            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, (uint)Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    bool value = KERNEL32.ResumeThread(processHandle);

                    if (value)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                finally
                {
                    KERNEL32.CloseHandle(processHandle);
                }
            }

            return -1;
        }

        public static Config GetConfigFromRegistry(string serviceName)
        {
            try
            {
                //Open Regkey folder
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName + @"\Parameters");

                //If the key doesn't exist
                if (key == null)
                    throw new Exception();

                Config config = new Config();

                //config.DisplayName = (string)key.GetValue("DisplayName");
                //config.Name = (string)key.GetValue("ServiceName");

                config.FileDir = (string)key.GetValue("FileDir");
                config.FileName = (string)key.GetValue("FileName");

                config.Parameter = (string)(key.GetValue("Parameter") ?? String.Empty);
                config.UserName = (string)(key.GetValue("UserName") ?? String.Empty);
                config.UserPassword = (string)(key.GetValue("UserPassword") ?? String.Empty);
                config.MaxRestarts = (int)(key.GetValue("MaxRestarts") ?? 3);

                config.ProcessKillTime = (int)(key.GetValue("ProcessKillTime") ?? 5000);
                config.ProcessRestartDelay = (int)(key.GetValue("ProcessRestartDelay") ?? 0);

                config.ConsoleApplication = Convert.ToBoolean((key.GetValue("ConsoleApplication") ?? false));
                config.UseCtrlC = Convert.ToBoolean((key.GetValue("UseCtrlC") ?? false));

                return config;
            }
            catch (Exception)
            {
                return null;
            }
        }


        //From: http://stackoverflow.com/questions/1841790/how-can-a-windows-service-determine-its-servicename, 02.05.2017
        public static String GetServiceName()
        {
            // Calling System.ServiceProcess.ServiceBase::ServiceNamea allways returns
            // an empty string,
            // see https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387024

            // So we have to do some more work to find out our service name, this only works if
            // the process contains a single service, if there are more than one services hosted
            // in the process you will have to do something else

            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            String query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            System.Management.ManagementObjectSearcher searcher =
                new System.Management.ManagementObjectSearcher(query);

            foreach (System.Management.ManagementObject queryObj in searcher.Get())
            {
                return queryObj["Name"].ToString();
            }

            throw new Exception("Can not get the ServiceName");
        }
    }
}
