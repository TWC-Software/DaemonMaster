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
using System.ServiceProcess;
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
            //Parser.Default.ParseArguments<ServiceOptions, GeneralOptions, EditOptions, InstallOptions, InstallDmdfOptions>(args)
            //   .MapResult(
            //        (ServiceOptions opts) => RunServiceAndReturnExitCode(opts),
            //       //(GeneralOptions opts) => RunOptionsAndReturnExitCode(opts),
            //       //(EditOptions opts) => RunEditReturnExitCode(opts),
            //       //(InstallOptions opts) => RunInstallAndReturnExitCode(opts),
            //       //(InstallDmdfOptions opts) => RunInstallDmdfAndReturnExitCode(opts),
            //       errs => 1);
            StartService();
        }

        private static int RunServiceAndReturnExitCode(ServiceOptions opts)
        {
            return StartService();
        }

        //private static int RunOptionsAndReturnExitCode(GeneralOptions option)
        //{
        //    int result = 0;

        //    //Check Admin right
        //    if (!SystemManagement.IsElevated())
        //    {
        //        Logger.Error("You must start the programm with admin rights.");
        //        return 1;
        //    }


        //    if (option.KillAllServices)
        //        if (ServiceManagement.KillAllServices())
        //            result = 1;

        //    if (option.DeleteAllServices)
        //        if (ServiceManagement.DeleteAllServices())
        //            result = 1;

        //    return result;
        //}

        //private static int RunEditReturnExitCode(EditOptions opts)
        //{
        //    //Check Admin right
        //    if (!SystemManagement.IsElevated())
        //    {
        //        Logger.Error("You must start the programm with admin rights.");
        //        return 1;
        //    }

        //    return InstallEditService(opts, opts.ServiceName, editMode: true);
        //}

        //private static int RunInstallAndReturnExitCode(InstallOptions opts)
        //{
        //    //Check Admin right
        //    if (!SystemManagement.IsElevated())
        //    {
        //        Logger.Error("You must start the programm with admin rights.");
        //        return 1;
        //    }

        //    return InstallEditService(opts, opts.ServiceName, editMode: false);
        //}

        //private static int RunInstallDmdfAndReturnExitCode(InstallDmdfOptions opts)
        //{
        //    //Check Admin right
        //    if (!SystemManagement.IsElevated())
        //    {
        //        Logger.Error("You must start the programm with admin rights.");
        //        return 1;
        //    }

        //    //Check Path
        //    if (String.IsNullOrWhiteSpace(opts.Path) || !File.Exists(opts.Path))
        //    {
        //        Logger.Error("Invalid or no path defined.");
        //        return 1;
        //    }

        //    return InstallNewServiceDmdf(opts.Path, opts.Password.ConvertStringToSecureString());
        //}


        //private static int InstallEditService(CommonEditInstallOptions opts, string serviceName, bool editMode)
        //{
        //    try
        //    {
        //        SecureString pw = opts.Password?.ConvertStringToSecureString();

        //        if (opts.CanInteractWithDesktop && !DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
        //        {
        //            Logger.Error("CanInteractWithDesktop is not supported in this windows version.");
        //            return 1;
        //        }
        //        if (opts.CanInteractWithDesktop && !String.IsNullOrWhiteSpace(opts.Username))
        //        {
        //            Logger.Error("CanInteractWithDesktop is not supported with custom user.");
        //            return 1;
        //        }
        //        if ((String.IsNullOrWhiteSpace(opts.Username) && pw != null) || (!String.IsNullOrWhiteSpace(opts.Username) && pw == null))
        //        {
        //            Logger.Error("Password/username parameter is missing!");
        //            return 1;
        //        }


        //        DmServiceDefinition serviceDefinition = new DmServiceDefinition(serviceName)
        //        {
        //            BinaryPath = opts.FullPath,
        //            DisplayName = opts.DisplayName,
        //            Description = opts.Description,
        //            Arguments = opts.Arguments,
        //            CanInteractWithDesktop = opts.CanInteractWithDesktop,
        //            ProcessMaxRestarts = opts.MaxRestarts,
        //            ProcessTimoutTime = opts.ProcessKillTime,
        //            ProcessRestartDelay = opts.ProcessRestartDelay,
        //            CounterResetTime = opts.CounterResetTime,
        //            IsConsoleApplication = opts.ConsoleApplication,
        //            UseCtrlC = opts.UseCtrlC
        //        };

        //        //Custom user
        //        if (!Equals(serviceDefinition.Credentials, ServiceCredentials.LocalSystem))
        //        {
        //            if (!SystemManagement.ValidateUser(opts.Username, pw))
        //            {
        //                Logger.Error("Failed to validate the given password/username.");
        //                return 1;
        //            }

        //            serviceDefinition.Credentials = new ServiceCredentials(opts.Username, pw);
        //        }

        //        //Set the start type
        //        switch (opts.StartType)
        //        {
        //            case 0:
        //                serviceDefinition.StartType = ServiceStartType.Disabled;
        //                serviceDefinition.DelayedStart = false;
        //                break;

        //            case 1:
        //                serviceDefinition.StartType = ServiceStartType.StartOnDemand;
        //                serviceDefinition.DelayedStart = false;
        //                break;

        //            case 2:
        //                serviceDefinition.StartType = ServiceStartType.AutoStart;
        //                serviceDefinition.DelayedStart = false;
        //                break;

        //            case 4:
        //                serviceDefinition.StartType = ServiceStartType.AutoStart;
        //                serviceDefinition.DelayedStart = true;
        //                break;

        //            default:
        //                Logger.Error("The StartType can only be between 0-4 (0 = Disabled / 1 = Demand start / 2 = Auto start / 4 = Delayed auto start).");
        //                return 1;

        //        }

        //        if (editMode)
        //        {
        //            ServiceManagement.ChangeServiceConfig(serviceDefinition);
        //        }
        //        else
        //        {
        //            ServiceManagement.CreateInteractiveService(serviceDefinition);
        //        }

        //        Logger.Info("Successful!");
        //        return 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.Message);
        //        return 1;
        //    }
        //}

        //private static int InstallNewServiceDmdf(string path, SecureString pw)
        //{
        //    try
        //    {
        //        DmServiceDefinition serviceDefinition = SystemManagement.ParseDmdfFile(path);

        //        //Check if the service already exist
        //        if (ServiceController.GetServices().Any(service => String.Equals(serviceDefinition.ServiceName, service.ServiceName)))
        //            throw new ArgumentException("A service with the same name already exist.");

        //        //Check if the password is valid when not the local system account is used
        //        if (!Equals(serviceDefinition.Credentials, ServiceCredentials.LocalSystem))
        //        {
        //            if (pw != null)
        //            {
        //                if (!SystemManagement.ValidateUser(serviceDefinition.Credentials.Username, pw))
        //                {
        //                    Logger.Error("Failed to validate the given password/username.");
        //                    return 1;
        //                }

        //                serviceDefinition.Credentials = new ServiceCredentials(serviceDefinition.Credentials.Username, pw); //Set password
        //            }
        //            else
        //            {
        //                throw new ArgumentException("You must give a password with the --pw parameter when you will use a custom user account.");
        //            }
        //        }

        //        ServiceManagement.CreateInteractiveService(serviceDefinition);

        //        Logger.Info("Successful!");
        //        return 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.Message);
        //        return 1;
        //    }
        //}

        private static int StartService()
        {
            var servicesToRun = new ServiceBase[]
            {
                new Service()
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
