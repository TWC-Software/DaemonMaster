using DaemonMasterCore.Win32.PInvoke.Advapi32;

namespace DaemonMasterCore.Win32
{
    /// <summary>
    /// An interface that represents all needed properties to create or change a win32 service
    /// </summary>
    public interface IWin32ServiceDefinition
    {
        /// <summary>
        /// Get or set the service name of the service
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// Get or set the display name of the service
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Get or set the path to the executable file
        /// </summary>
        string BinaryPath { get; set; }

        /// <summary>
        /// Get or set the description of the service
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Get or set the start type of the service (like: automatic)
        /// </summary>
        Advapi32.ServiceStartType StartType { get; set; }

        /// <summary>
        /// Get or set the delayed start parameter
        /// </summary>
        bool DelayedStart { get; set; }

        /// <summary>
        /// Gets or sets the arguments that are given to the executable file on start
        /// </summary>
        string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the error control of the service
        /// </summary>
        Advapi32.ErrorControl ErrorControl { get; set; }

        /// <summary>
        /// Get or set the service logon credentials (like: LocalSystem)
        /// </summary>       
        ServiceCredentials Credentials { get; set; }

        //----------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the services on which this one depends
        /// </summary>
        string[] DependOnService { get; set; }

        /// <summary>
        /// Gets or sets the service gruppes on which this one depends
        /// </summary>
        string[] DependOnGroup { get; set; }

        /// <summary>
        /// Get or set the can intercat with desktop parameter
        /// </summary>
        bool CanInteractWithDesktop { get; set; }

    }
}
