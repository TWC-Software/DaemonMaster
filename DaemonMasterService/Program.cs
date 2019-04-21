using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.ServiceProcess;
using CommandLine;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using NLog;

namespace DaemonMasterService
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ServiceOptions, GeneralOptions, EditOptions, InstallOptions>(args)
               .MapResult(
                    (ServiceOptions opts) => RunServiceAndReturnExitCode(opts),
                   (GeneralOptions opts) => RunOptionsAndReturnExitCode(opts),
                   (EditOptions opts) => RunEditReturnExitCode(opts),
                   (InstallOptions opts) => RunInstallAndReturnExitCode(opts),
                   errs => 1);
        }

        private static int RunServiceAndReturnExitCode(ServiceOptions opts)
        {
            return StartService();
        }

        private static int RunOptionsAndReturnExitCode(GeneralOptions option)
        {
            int result = 0;

            //Check Admin right
            if (!DaemonMasterUtils.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }


            //TODO

            return result;
        }

        private static int RunEditReturnExitCode(EditOptions opts)
        {
            //Check Admin right
            if (!DaemonMasterUtils.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }

            return InstallEditService(opts, opts.ServiceName, editMode: true);
        }

        private static int RunInstallAndReturnExitCode(InstallOptions opts)
        {
            //Check Admin right
            if (!DaemonMasterUtils.IsElevated())
            {
                Logger.Error("You must start the programm with admin rights.");
                return 1;
            }

            return InstallEditService(opts, opts.ServiceName, editMode: false);
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

                if (opts.CanInteractWithDesktop && (!string.IsNullOrWhiteSpace(opts.Username) || pw != null))
                {
                    Logger.Error("CanInteractWithDesktop is not supported with custom user.");
                    return 1;
                }

                if ((string.IsNullOrWhiteSpace(opts.Username) && pw != null) || (!string.IsNullOrWhiteSpace(opts.Username) && pw == null))
                {
                    Logger.Error("Password/username parameter is missing!");
                    return 1;
                }


                var serviceDefinition = new DmServiceDefinition("DaemonMaster_" + serviceName)
                {
                    BinaryPath = opts.FullPath,
                    DisplayName = opts.DisplayName,
                    Description = opts.Description,
                    Arguments = opts.Arguments,
                    LoadOrderGroup = opts.LoadOrderGroup,
                    CanInteractWithDesktop = opts.CanInteractWithDesktop,
                    ProcessMaxRestarts = opts.MaxRestarts,
                    ProcessTimeoutTime = opts.ProcessTimeoutTime,
                    ProcessRestartDelay = opts.ProcessRestartDelay,
                    CounterResetTime = opts.CounterResetTime,
                    IsConsoleApplication = opts.ConsoleApplication,
                    UseCtrlC = opts.UseCtrlC
                };

                //Custom user
                //Create new ServiceCredentials instance
                if (!string.IsNullOrWhiteSpace(opts.Username) && pw != null)
                {
                    //if (!DaemonMasterUtils.ValidateUser(opts.Username, pw))
                    //{
                    //    Logger.Error("Failed to validate the given password/username.");
                    //    return 1;
                    //}

                    serviceDefinition.Credentials = new ServiceCredentials(opts.Username, pw);

                    //Check if he has the right to start as service
                    using (LsaPolicyHandle lsaWrapper = LsaPolicyHandle.OpenPolicyHandle())
                    {
                        bool hasRightToStartAsService = lsaWrapper.EnumeratePrivileges(serviceDefinition.Credentials.Username).Any(x => x.Buffer == "SeServiceLogonRight");
                        if (!hasRightToStartAsService)
                        {
                            Logger.Error("The user doesn't have the right to start as service. Do you want to give him that right? [Yes/No]");
                            switch (Console.ReadLine())
                            {
                                case "yes":
                                case "Yes":
                                case "y":
                                case "Y":
                                    //Give the account the right to start as service
                                    lsaWrapper.AddPrivileges(serviceDefinition.Credentials.Username, "SeServiceLogonRight");
                                    break;

                                default:
                                    Logger.Error("Cannot create the service without that right.");
                                    return 1;
                            }
                        }
                    }
                }

                //Set the start type
                switch (opts.StartType)
                {
                    case 0:
                        serviceDefinition.StartType = Advapi32.ServiceStartType.Disabled;
                        serviceDefinition.DelayedStart = false;
                        break;

                    case 1:
                        serviceDefinition.StartType = Advapi32.ServiceStartType.StartOnDemand;
                        serviceDefinition.DelayedStart = false;
                        break;

                    case 2:
                        serviceDefinition.StartType = Advapi32.ServiceStartType.AutoStart;
                        serviceDefinition.DelayedStart = false;
                        break;

                    case 4:
                        serviceDefinition.StartType = Advapi32.ServiceStartType.AutoStart;
                        serviceDefinition.DelayedStart = true;
                        break;

                    default:
                        Logger.Error("The StartType can only be between 0-4 (0 = Disabled / 1 = Demand start / 2 = Auto start / 4 = Delayed auto start).");
                        return 1;
                }

                //Set the start type
                switch (opts.ProcessPriority)
                {
                    case 0:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.Idle;
                        break;

                    case 1:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.BelowNormal;
                        break;

                    case 2:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.Normal;
                        break;

                    case 4:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.AboveNormal;
                        break;

                    case 5:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.High;
                        break;

                    case 6:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.RealTime;
                        break;

                    default:
                        Logger.Error("The ProcessPriority can only be between 0-6 (0 = Idle / 1 = Below normal / 2 = Normal / 4 = Above normal / 5 = High / 6 = Real time (not recommended to use)).");
                        return 1;
                }

                if (editMode)
                {
                    using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                    {
                        using (ServiceHandle service = scm.OpenService(serviceDefinition.ServiceName, Advapi32.ServiceAccessRights.AllAccess))
                        {
                            if (service.QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                            {
                                Logger.Error("Service is not stopped, please stop it first.");
                                return 1;
                            }

                            service.ChangeConfig(serviceDefinition);
                        }
                    }
                }
                else
                {
                    using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.CreateService))
                    {
                        scm.CreateService(serviceDefinition);
                    }
                }

                //Save in registry
                RegistryManagement.SaveInRegistry(serviceDefinition);

                Logger.Info("Successful!");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                return 1;
            }
        }

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
