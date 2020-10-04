using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using CommandLine;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace DaemonMasterService
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Entry point of the application
        /// </summary>
        private static void Main(string[] args)
        {
            try
            {
                SetupNLog();

                Parser.Default.ParseArguments<ServiceOptions, GeneralOptions, EditOptions, InstallOptions, ListOptions>(args) //Type must be a console app not a windows app
                    .MapResult(
                        (ServiceOptions opts) => RunServiceAndReturnExitCode(opts),
                        (GeneralOptions opts) => RunOptionsAndReturnExitCode(opts),
                        (EditOptions opts) => RunEditReturnExitCode(opts),
                        (InstallOptions opts) => RunInstallAndReturnExitCode(opts),
                        (ListOptions opts) => RunListServicesAndReturnExitCode(opts),
                        errs => 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //Shutdown all logger
            LogManager.Shutdown();
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
                Console.WriteLine("You must start the program with admin rights.");
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
                Console.WriteLine("You must start the program with admin rights.");
                return 1;
            }

            //------------------------

            DmServiceDefinition serviceDefinition;
            try
            {
                if (string.IsNullOrWhiteSpace(opts.ServiceName))
                {
                    Console.WriteLine("The given service name is invalid.");
                    return -1;
                }

                string realServiceName = opts.ServiceName;
                if (!RegistryManagement.IsDaemonMasterService(realServiceName))
                {
                    realServiceName = "DaemonMaster_" + realServiceName; //Check for the name of the old system TODO: remove later
                    if (!RegistryManagement.IsDaemonMasterService(realServiceName))
                    {
                        Console.WriteLine("Cannot found a DaemonMaster service with the given name.");
                        return -1;
                    }
                }

                //Load data from registry
                serviceDefinition = RegistryManagement.LoadFromRegistry(realServiceName);
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot found a service with the given service name."); //"\n" + e.Message + "\n StackTrace: " + e.StackTrace);
                return 1;
            }

            try
            {
                CheckAndSetCommonArguments(ref serviceDefinition, opts);

                //Edit service
                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle service = scm.OpenService(serviceDefinition.ServiceName, Advapi32.ServiceAccessRights.AllAccess))
                    {
                        if (service.QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                        {
                            Console.WriteLine("Service is not stopped, please stop it first.");
                            return 1;
                        }

                        service.ChangeConfig(serviceDefinition);
                    }
                }

                //Save arguments in registry
                RegistryManagement.SaveInRegistry(serviceDefinition);

                Console.WriteLine("Successful!");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int RunInstallAndReturnExitCode(InstallOptions opts)
        {
            //Check Admin right
            if (!DaemonMasterUtils.IsElevated())
            {
                Console.WriteLine("You must start the program with admin rights.");
                return 1;
            }

            //------------------------

            if (string.IsNullOrWhiteSpace(opts.ServiceName))
            {
                Console.WriteLine("The given service name is invalid.");
                return -1;
            }

            var serviceDefinition = new DmServiceDefinition(opts.ServiceName)
            {
                BinaryPath = opts.FullPath,
                DisplayName = opts.DisplayName
            };

            try
            {
                CheckAndSetCommonArguments(ref serviceDefinition, opts);

                //Install service
                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.CreateService))
                {
                    scm.CreateService(serviceDefinition);
                }

                //Save arguments in registry
                RegistryManagement.SaveInRegistry(serviceDefinition);

                Console.WriteLine("Successful!");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int RunListServicesAndReturnExitCode(ListOptions opts)
        {
            //Check Admin right
            if (!DaemonMasterUtils.IsElevated())
            {
                Console.WriteLine("You must start the program with admin rights.");
                return 1;
            }

            try
            {
                Console.WriteLine("Number:  service name / display name");

                List<DmServiceDefinition> services = RegistryManagement.GetInstalledServices();
                for (var i = 0; i < services.Count; i++)
                {
                    var sb = new StringBuilder();
                    sb.Append(i);
                    sb.Append(": ");
                    sb.Append(services[i].ServiceName.Contains("DaemonMaster_") ? services[i].ServiceName.Remove(0, 13) : services[i].ServiceName); //Remove internally used prefix TODO: remove that on a later release
                    sb.Append(" / ");
                    sb.Append(services[i].DisplayName);
                    Console.WriteLine(sb);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void CheckAndSetCommonArguments(ref DmServiceDefinition serviceDefinition, CommonEditInstallOptions opts)
        {

            serviceDefinition.Description = opts.Description ?? serviceDefinition.Description;
            serviceDefinition.Arguments = opts.Arguments ?? serviceDefinition.Arguments;
            serviceDefinition.LoadOrderGroup = opts.LoadOrderGroup ?? serviceDefinition.LoadOrderGroup;
            serviceDefinition.CanInteractWithDesktop = opts.CanInteractWithDesktop ?? serviceDefinition.CanInteractWithDesktop;
            serviceDefinition.ProcessMaxRestarts = opts.MaxRestarts ?? serviceDefinition.ProcessMaxRestarts;
            serviceDefinition.ProcessTimeoutTime = opts.ProcessTimeoutTime ?? serviceDefinition.ProcessTimeoutTime;
            serviceDefinition.ProcessRestartDelay = opts.ProcessRestartDelay ?? serviceDefinition.ProcessRestartDelay;
            serviceDefinition.CounterResetTime = opts.CounterResetTime ?? serviceDefinition.CounterResetTime;
            serviceDefinition.IsConsoleApplication = opts.ConsoleApplication ?? serviceDefinition.IsConsoleApplication;
            serviceDefinition.UseCtrlC = opts.UseCtrlC ?? serviceDefinition.UseCtrlC;
            serviceDefinition.Credentials = new ServiceCredentials(opts.Username, opts?.Password?.ConvertStringToSecureString());

            if (opts.StartType != null)
            {
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
                        throw new ArgumentException("The StartType can only be between 0-4 (0 = Disabled / 1 = Demand start / 2 = Auto start / 4 = Delayed auto start).");
                }
            }

            if (opts.ProcessPriority != null)
            {
                switch (opts.ProcessPriority)
                {
                    case -2:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.Idle;
                        break;

                    case -1:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.BelowNormal;
                        break;

                    case 0:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.Normal;
                        break;

                    case 1:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.AboveNormal;
                        break;

                    case 2:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.High;
                        break;

                    case 3:
                        serviceDefinition.ProcessPriority = ProcessPriorityClass.RealTime;
                        break;

                    default:
                        throw new ArgumentException("The ProcessPriority can only be between -2<->3 (-2 = Idle / -1 = Below normal / 0 = Normal / 1 = Above normal / 2 = High / 3 = Real time (not recommended to use)).");
                }
            }


            if (serviceDefinition.CanInteractWithDesktop && !DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
            {
                throw new ArgumentException("CanInteractWithDesktop is not supported in this windows version.");
            }

            if (serviceDefinition.CanInteractWithDesktop && (!string.IsNullOrWhiteSpace(serviceDefinition.Credentials.Username) || serviceDefinition.Credentials.Password != null))
            {
                throw new ArgumentException("CanInteractWithDesktop is not supported with custom user.");
            }

            if ((string.IsNullOrWhiteSpace(serviceDefinition.Credentials.Username) && serviceDefinition.Credentials.Password != null) || (!string.IsNullOrWhiteSpace(serviceDefinition.Credentials.Username) && serviceDefinition.Credentials == null))
            {
                throw new ArgumentException("Password/username parameter is missing!");
            }


            //Custom user
            //Create new ServiceCredentials instance
            if (!string.IsNullOrWhiteSpace(serviceDefinition.Credentials.Username) && serviceDefinition.Credentials.Password != null && !ServiceCredentials.IsVirtualAccount(serviceDefinition.Credentials))
            {
                //Check if he has the right to start as service
                using (LsaPolicyHandle lsaWrapper = LsaPolicyHandle.OpenPolicyHandle())
                {
                    bool hasRightToStartAsService = lsaWrapper.EnumeratePrivileges(serviceDefinition.Credentials.Username).Any(x => x.Buffer == "SeServiceLogonRight");
                    if (!hasRightToStartAsService)
                    {
                        Console.WriteLine("The user doesn't have the right to start as service. Do you want to give him that right? [Yes/No]");
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
                                throw new ArgumentException("Cannot create the service without that right.");
                        }
                    }
                }
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
                Logger.Error(ex, "Failed to start the service: " + ex.Message);
                return 1;
            }
        }

        private static void SetupNLog()
        {
            LogManager.KeepVariablesOnReload = true;

            //Create configuration object 
            var config = new LoggingConfiguration();

            //Define log name
            if (config.Variables.ContainsKey("logName"))
            {
                config.Variables["logName"] = "DaemonMaster_Service";
            }
            else
            {
                config.Variables.Add("logName", SimpleLayout.Escape("DaemonMaster_Service"));
            }

            //Create targets and adding rules
            var consoleTarget = new ColoredConsoleTarget("consoleTarget")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${level:uppercase=true} ${message} ${newline} ${exception:format=ToString}",
                DetectConsoleAvailable = true
            };
            config.AddTarget(consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);// only infos and higher

            //#if DEBUG
            //            var debugFileTarget = new FileTarget("debugFileTarget")
            //            {
            //                FileName = @"${basedir}\logs\Debug_${logDir}${var:logName}.${shortdate}.log",
            //                Layout = @"${longdate}|${level:uppercase=true}|${exception:format=ToString,StackTrace}|${logger}|${message}",
            //                ArchiveOldFileOnStartup = true,
            //                ArchiveFileName = @"${basedir}\logs\archive\Debug_${archiveDir}${var:logName}.${shortdate}.{#####}.log",
            //                ArchiveNumbering = ArchiveNumberingMode.Sequence,
            //                MaxArchiveFiles = 10
            //            };
            //            config.AddTarget(debugFileTarget);
            //            config.AddRuleForAllLevels(debugFileTarget);
            //#else
            //            var fileTarget = new FileTarget("fileTarget")
            //            {
            //                FileName = @"${basedir}\logs\${logDir}${var:logName}.${shortdate}.log",
            //                Layout = @"${longdate}|${level:uppercase=true}|${exception:format=ToString,StackTrace}|${logger}|${message}",
            //                ArchiveOldFileOnStartup = true,
            //                ArchiveFileName = @"${basedir}\logs\archive\${var:logName}.${shortdate}.{#####}.log",
            //                ArchiveNumbering = ArchiveNumberingMode.Sequence,
            //                MaxArchiveFiles = 10
            //            };
            //            config.AddTarget(fileTarget);
            //            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget); // only infos and higher
            //#endif

            //Activate the configuration
            LogManager.Configuration = config;
            LogManager.ReconfigExistingLoggers();
        }
    }
}
