using CommandLine;

namespace DaemonMasterService
{
    public abstract class GlobalOptions
    {

    }

    [Verb("general", HelpText = "General options like deleteAllServices, etc.")]
    public class GeneralOptions : GlobalOptions
    {
        //[Option("deleteAllServices", Required = false, HelpText = "Delete all installed services that are using this .exe to work.", Default = false)]
        //public bool DeleteAllServices { get; set; }

        //[Option("killAllServices", Required = false, HelpText = "Kill all installed services that are using this .exe to work.", Default = false)]
        //public bool KillAllServices { get; set; }
    }

    [Verb("service", HelpText = "Start the .exe in service mode.")]
    public class ServiceOptions : GlobalOptions
    {
    }


    public abstract class CommonEditInstallOptions : GlobalOptions
    {
        [Option("description", Required = false, HelpText = "Here you can give the service a description.", Default = null)]
        public string Description { get; set; }


        [Option("args", Required = false, HelpText = "Here you can give arguments that  ar for the service.", Default = null)]
        public string Arguments { get; set; }

        [Option("loadOrderGroup", Required = false, HelpText = "Set the load order group.", Default = null)]
        public string LoadOrderGroup { get; set; }


        [Option("username", SetName = "CustomUser", Required = false, HelpText = "Only local users (Example: '.\\Alfred').", Default = null)]
        public string Username { get; set; }

        [Option("pw", SetName = "CustomUser", Required = false, HelpText = "Password if it should start with custom user account.", Default = null)]
        public string Password { get; set; }


        [Option("maxRestarts", Required = false, HelpText = "Max number of restarts after that the service stops.", Default = null)]
        public int? MaxRestarts { get; set; }

        [Option("startType", Required = false, HelpText = "The start type of the service (0 = Disabled / 1 = Demand start / 2 = Auto start / 4 = Delayed auto start).", Default = null)]
        public int? StartType { get; set; }

        [Option("processPriority", Required = false, HelpText = "The priority of the process that the service starts (0 = Idle / 1 = Below normal / 2 = Normal / 4 = Above normal / 5 = High / 6 = Real time (not recommended to use)).", Default = null)]
        public int? ProcessPriority { get; set; }

        [Option("processTimeoutTime", Required = false, HelpText = "Time after a stop or start command that trigger an timeout error.", Default = null)]
        public int? ProcessTimeoutTime { get; set; }

        [Option("processRestartDelay", Required = false, HelpText = "Delay between a crash and the restart of the process.", Default = null)]
        public int? ProcessRestartDelay { get; set; }

        [Option("counterResetTime", Required = false, HelpText = "Time after that the restarts counter gets reset.", Default = null)]
        public int? CounterResetTime { get; set; }

        [Option("consoleApp", Required = false, HelpText = "If true the process becomes firstly an ctrl-c or ctrl-break command to close.", Default = null)]
        public bool? ConsoleApplication { get; set; }

        [Option("ctrlC", Required = false, HelpText = "If it uses the ctrl-c key (if false then it uses the ctrl--break key).", Default = null)]
        public bool? UseCtrlC { get; set; }

        [Option("canInteractWithDesktop", SetName = "LocalUser", Required = false, HelpText = "Allows the service to interact with the desktop (Can only be used with supported windows versions and also NOT with a custom user).", Default = null)]
        public bool? CanInteractWithDesktop { get; set; }
    }

    [Verb("edit", HelpText = "Edit an installed service.")]
    public class EditOptions : CommonEditInstallOptions
    {
        [Option("serviceName", Required = true, HelpText = "Name of the the service that should be edited.")]
        public string ServiceName { get; set; }

        [Option("displayName", Required = false, HelpText = "Display name of the service in Windows/DaemonMaster.", Default = null)]
        public string DisplayName { get; set; }

        [Option("fullPath", Required = false, HelpText = "The full path to the file that should started as service", Default = null)]
        public string FullPath { get; set; }
    }

    [Verb("install", HelpText = "Create a new service.")]
    public class InstallOptions : CommonEditInstallOptions
    {
        [Option("serviceName", Required = true, HelpText = "Name of the new service")]
        public string ServiceName { get; set; }

        [Option("displayName", Required = true, HelpText = "Display name of the service in Windows/DaemonMaster.")]
        public string DisplayName { get; set; }

        [Option("fullPath", Required = true, HelpText = "The full path to the file that should started as service")]
        public string FullPath { get; set; }
    }

    [Verb("list", HelpText = "List all found DaemonMaster services and its service/display name.")]
    public class ListOptions
    {
    }
}
