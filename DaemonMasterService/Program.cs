/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: Program
//  
//  This file is part of DeamonMaster.
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
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.ServiceProcess;
using CommandLine;
using DaemonMasterCore;
using DaemonMasterCore.Win32.PInvoke;
using NLog;

namespace DaemonMasterService
{
    static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<GeneralOptions, ServiceOptions, EditOptions, InstallOptions, InstallDmdfOptions>(args)
               .MapResult(
                   (GeneralOptions opts) => RunOptionsAndReturnExitCode(opts),
                   (ServiceOptions opts) => RunServiceAndReturnExitCode(opts),
                   (EditOptions opts) => RunEditReturnExitCode(opts),
                   (InstallOptions opts) => RunInstallAndReturnExitCode(opts),
                   (InstallDmdfOptions opts) => RunInstallDmdfAndReturnExitCode(opts),
                   errs => 1);
        }

        private static int RunServiceAndReturnExitCode(ServiceOptions opts)
        {
            return StartService(opts.EnablePause);
        }

        private static int RunOptionsAndReturnExitCode(GeneralOptions option)
        {
            int result = 0;

            //Check Admin right
            if (!SystemManagement.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }


            if (option.KillAllServices)
                if (ServiceManagement.KillAllServices())
                    result = 1;

            if (option.DeleteAllServices)
                if (ServiceManagement.DeleteAllServices())
                    result = 1;

            return result;
        }

        private static int RunEditReturnExitCode(EditOptions opts)
        {
            //Check Admin right
            if (!SystemManagement.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }

            return InstallEditService(opts, opts.ServiceName, editMode: true);
        }

        private static int RunInstallAndReturnExitCode(InstallOptions opts)
        {
            //Check Admin right
            if (!SystemManagement.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }

            return InstallEditService(opts, opts.ServiceName, editMode: false);
        }

        private static int RunInstallDmdfAndReturnExitCode(InstallDmdfOptions opts)
        {
            //Check Admin right
            if (!SystemManagement.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }

            if (!String.IsNullOrWhiteSpace(opts.Path) && File.Exists(opts.Path))
            {
                Logger.Error("Invalid or no path defined.");
                return 1;
            }

            return InstallNewServiceDmdf(opts.Path, opts.Password.ConvertStringToSecureString());
        }


        private static int InstallEditService(CommonEditInstallOptions opts, string serviceName, bool editMode)
        {
            try
            {
                SecureString pw = opts.Password?.ConvertStringToSecureString();

                if (opts.CanInteractWithDesktop && !DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
                {
                    Logger.Error("CanInteractWithDesktop is not supported in this windows version.");
                    return 1;
                }
                if (opts.CanInteractWithDesktop && !String.IsNullOrWhiteSpace(opts.Username))
                {
                    Logger.Error("CanInteractWithDesktop is not supported with custom user.");
                    return 1;
                }
                if ((String.IsNullOrWhiteSpace(opts.Username) && pw != null) || (!String.IsNullOrWhiteSpace(opts.Username) && pw == null))
                {
                    Logger.Error("Password/username parameter is missing!");
                    return 1;
                }


                ServiceStartInfo serviceStartInfo = new ServiceStartInfo
                {
                    FullPath = opts.FullPath,
                    ServiceName = serviceName,
                    DisplayName = opts.DisplayName,
                    Description = opts.Description,
                    Parameter = opts.Arguments,
                    CanInteractWithDesktop = opts.CanInteractWithDesktop,
                    UseLocalSystem = String.IsNullOrWhiteSpace(opts.Username) && pw == null,
                    MaxRestarts = opts.MaxRestarts,
                    ProcessKillTime = opts.ProcessKillTime,
                    ProcessRestartDelay = opts.ProcessRestartDelay,
                    CounterResetTime = opts.CounterResetTime,
                    ConsoleApplication = opts.ConsoleApplication,
                    UseCtrlC = opts.UseCtrlC
                };

                //Custom user
                if (serviceStartInfo.UseLocalSystem)
                {
                    serviceStartInfo.Username = editMode ? "LocalSystem" : null; //Null stand in edit mode for no change so "LocalSystem" must be used there
                    serviceStartInfo.Password = null;
                }
                else
                {
                    if (!SystemManagement.ValidateUser(opts.Username, pw))
                    {
                        Logger.Error("Failed to validate the given password/username.");
                        return 1;
                    }

                    serviceStartInfo.Username = opts.Username;
                    serviceStartInfo.Password = pw;
                }

                //Set the start type
                switch (opts.StartType)
                {
                    case 0:
                        serviceStartInfo.StartType = NativeMethods.SERVICE_START.SERVICE_DISABLED;
                        serviceStartInfo.DelayedStart = false;
                        break;

                    case 1:
                        serviceStartInfo.StartType = NativeMethods.SERVICE_START.SERVICE_DEMAND_START;
                        serviceStartInfo.DelayedStart = false;
                        break;

                    case 2:
                        serviceStartInfo.StartType = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
                        serviceStartInfo.DelayedStart = false;
                        break;

                    case 4:
                        serviceStartInfo.StartType = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
                        serviceStartInfo.DelayedStart = true;
                        break;

                    default:
                        Logger.Error("The StartType can only be between 0-4 (0 = Disabled / 1 = Demand start / 2 = Auto start / 4 = Delayed auto start).");
                        return 1;

                }

                if (editMode)
                {
                    ServiceManagement.ChangeServiceConfig(serviceStartInfo);
                }
                else
                {
                    ServiceManagement.CreateInteractiveService(serviceStartInfo);
                }

                Logger.Info("Successful!");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return 1;
            }
        }

        private static int InstallNewServiceDmdf(string path, SecureString pw)
        {
            try
            {
                ServiceStartInfo serviceStartInfo = SystemManagement.ParseDmdfFile(path);

                //Check if the service already exist
                if (ServiceController.GetServices().Any(service => String.Equals(serviceStartInfo.ServiceName, service.ServiceName)))
                    throw new ArgumentException("A service with the same name already exist.");

                //Check if the password is valid when not the local system account is used
                if (!serviceStartInfo.UseLocalSystem)
                {
                    if (pw != null && pw.Length > 0)
                    {
                        serviceStartInfo.Password = pw; //Set password
                    }
                    else
                    {
                        throw new ArgumentException("You must give a password with the --pw parameter when you will use a custom user account.");
                    }
                }

                ServiceManagement.CreateInteractiveService(serviceStartInfo);

                Logger.Info("Successful!");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return 1;
            }
        }

        private static int StartService(bool enablePause)
        {
            var servicesToRun = new ServiceBase[]
            {
                new Service(enablePause)
            };

            try
            {
                ServiceBase.Run(servicesToRun);
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start the service: \n" + ex.Message);
                return 1;
            }
        }
    }
}
