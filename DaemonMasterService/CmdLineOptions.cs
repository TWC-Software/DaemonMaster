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
        [Option('p', "enablePause", Required = false, HelpText = "Enable the pause function (so that the service can pause / NOT recommended).", Default = false)]
        public bool EnablePause { get; set; }
    }

    [Verb("install", HelpText = "Create a new service.")]
    public class InstallOptions : GlobalOptions
    {
        [Option("dmdfFile", Required = false, HelpText = "Path to the dmdf file (JSON format).", Default = "")]
        public string DmdfFile { get; set; }

        [Option("pw", Required = false, HelpText = "Password if it should start with custom user account.", Default = null)]
        public string Password { get; set; }
    }
}
