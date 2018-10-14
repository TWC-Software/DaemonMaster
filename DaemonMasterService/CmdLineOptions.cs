using CommandLine;

namespace DaemonMasterService
{
    public abstract class GlobalOptions
    {

    }

    [Verb("general", HelpText = "General options like deleteAllServices, etc.")]
    public class GeneralOptions : GlobalOptions
    {
        [Option("deleteAllServices", Required = false, HelpText = "Delete all installed services that are using this .exe to work.", Default = false)]
        public bool DeleteAllServices { get; set; }

        [Option("killAllServices", Required = false, HelpText = "Kill all installed services that are using this .exe to work.", Default = false)]
        public bool KillAllServices { get; set; }
    }

    [Verb("service", HelpText = "Start the .exe in service mode.")]
    public class ServiceOptions : GlobalOptions
    {
        [Option('p', "enablePause", Required = false, HelpText = "Enable the pause function (so that the service can be paused / NOT recommended).", Default = false)]
        public bool EnablePause { get; set; }
    }

    [Verb("install", HelpText = "Create a new service.")]
    public class InstallOptions : GlobalOptions
    {
        //Dmdf installer parameters
        [Option("dmdfFile", SetName = "dmdfInstaller", Required = true, HelpText = "Path to the dmdf file (JSON format).", Default = "")]
        public string DmdfFile { get; set; }


        //Normal installer parameters
        [Option("serviceName", SetName = "normalInstaller", Required = true, HelpText = "", Default = null)]
        public string ServiceName { get; set; }

        [Option("displayName", SetName = "normalInstaller", Required = true, HelpText = "", Default = null)]
        public string DisplayName { get; set; }

        [Option("description", SetName = "normalInstaller", Required = false, HelpText = "Here you can give the service a description.", Default = "")]
        public string Description { get; set; }


        [Option("args", SetName = "normalInstaller", Required = false, HelpText = "Here you can give argumments that  ar for the service.", Default = "")]
        public string Arguments { get; set; }

        [Option("fullPath", SetName = "normalInstaller", Required = true, HelpText = "The full path to the file that should started as service", Default = null)]
        public string FullPath { get; set; }


        [Option("username", SetName = "normalInstaller", Required = false, HelpText = "", Default = "")]
        public string Username { get; set; }


        [Option("maxRestarts", SetName = "normalInstaller", Required = false, HelpText = "", Default = 3)]
        public int MaxRestarts { get; set; }

        [Option("startType", SetName = "normalInstaller", Required = false, HelpText = "", Default = 1)]
        public int StartType { get; set; }

        [Option("processKillTime", SetName = "normalInstaller", Required = false, HelpText = "", Default = 9500)]
        public int ProcessKillTime { get; set; }

        [Option("processRestartDelay", SetName = "normalInstaller", Required = false, HelpText = "", Default = 2000)]
        public int ProcessRestartDelay { get; set; }

        [Option("counterResetTime", SetName = "normalInstaller", Required = false, HelpText = "", Default = 43200)]
        public int CounterResetTime { get; set; }

        [Option("delayedStart", SetName = "normalInstaller", Required = false, HelpText = "", Default = false)]
        public bool DelayedStart { get; set; }

        [Option("consoleApp", SetName = "normalInstaller", Required = false, HelpText = "", Default = false)]
        public bool ConsoleApplication { get; set; }

        [Option("ctrlC", SetName = "normalInstaller", Required = false, HelpText = "", Default = false)]
        public bool UseCtrlC { get; set; }

        [Option("canInteractWithDesktop", SetName = "normalInstaller", Required = false, HelpText = "", Default = false)]
        public bool CanInteractWithDesktop { get; set; }


        //Common parameters
        [Option("pw", Required = false, HelpText = "Password if it should start with custom user account.", Default = null)]
        public string Password { get; set; }
    }
}
