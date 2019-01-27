using DaemonMaster.Core.Win32.PInvoke.Advapi32;

namespace DaemonMaster.Core.Win32
{
    /// <summary>
    /// An interface that represents all needed properties to create or change a win32 service
    /// </summary>
    public interface IWin32ServiceDefinition
    {
        /// <summary>
        /// Gets or sets the service name of the service
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the service
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the path to the executable file
        /// </summary>
        string BinaryPath { get; set; }

        /// <summary>
        /// Gets or sets the description of the service
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets the start type of the service (like: automatic)
        /// </summary>
        Advapi32.ServiceStartType StartType { get; set; }

        /// <summary>
        /// Gets or sets the delayed start parameter
        /// </summary>
        bool DelayedStart { get; set; }

        /// <summary>
        /// Gets or sets the arguments that are given to the executable file on start
        /// </summary>
        string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the load order group.
        /// </summary>
        /// <value>
        /// The load order group.
        /// </value>
        string LoadOrderGroup { get; set; }

        /// <summary>
        /// Gets or sets the error control of the service
        /// </summary>
        Advapi32.ErrorControl ErrorControl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the failure actions will be called even when the service reports that it is stopped but with a return code diffrent from zero.
        /// The change takes effect the next time the system is started.
        /// </summary>
        /// <value>
        /// When <c>true</c>, failure actions will be called even when the service reports that it is stopped but with a return code diffrent from zero.
        /// When <c>false</c>, failure actions will only be called when the service terminates without reporting an exit code.
        /// </value>
        bool FailureActionsOnNonCrashFailures { get; set; }

        /// <summary>
        /// Gets or sets the failure actions for the service.
        /// </summary>
        /// <value>
        /// The failure actions.
        /// </value>
        Advapi32.ServiceFailureActions FailureActions { get; set; }

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
        /// Gets or sets the can intercat with desktop parameter
        /// </summary>
        bool CanInteractWithDesktop { get; set; }

    }
}
