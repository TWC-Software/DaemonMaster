using System;
using System.Collections.Generic;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public struct ServiceFailureActions : IEquatable<ServiceFailureActions>
        {
            /// <summary>
            ///  Time that is necessary after the last failure, to reset the failure count.
            /// </summary>
            public TimeSpan ResetPeriode { get; }

            /// <summary>
            /// The message to be broadcast to server users before rebooting (only when a reboot action failure is configured).
            /// If this value is NULL, the reboot message is unchanged. If the value is an empty string (""), the reboot message is deleted and no message is broadcast.
            /// </summary>
            public string RebootMessage { get; }

            /// <summary>
            /// The command line of the process.
            /// If this value is NULL, the command is unchanged. If the value is an empty string (""), the command is deleted and no program is run when the service fails.
            /// This process runs under the same account as the service.
            /// </summary>
            public string Command { get; }

            /// <summary>
            /// Array of actions.
            /// When this value is null <see cref="ResetPeriode"/> are ignored.
            /// </summary>
            public IReadOnlyList<ScAction> Actions { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ServiceFailureActions"/> struct.
            /// </summary>
            /// <param name="resetPeriode">The reset periode.</param>
            /// <param name="rebootMessage">The reboot message.</param>
            /// <param name="command">The command.</param>
            /// <param name="actions">The actions.</param>
            public ServiceFailureActions(TimeSpan resetPeriode, string rebootMessage, string command, IReadOnlyList<ScAction> actions)
            {
                ResetPeriode = resetPeriode;
                RebootMessage = rebootMessage;
                Command = command;
                Actions = actions;
            }

            /// <summary>
            /// The default values from windows.
            /// </summary>
            public static ServiceFailureActions Default = new ServiceFailureActions(TimeSpan.Zero, null, null, null);


            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
            /// </returns>
            public bool Equals(ServiceFailureActions other)
            {
                return ResetPeriode.Equals(other.ResetPeriode) && string.Equals(RebootMessage, other.RebootMessage) && string.Equals(Command, other.Command) && Equals(Actions, other.Actions);
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
            /// <returns>
            ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
            /// </returns>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ServiceFailureActions actions && Equals(actions);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = ResetPeriode.GetHashCode();
                    hashCode = (hashCode * 397) ^ (RebootMessage != null ? RebootMessage.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Command != null ? Command.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Actions != null ? Actions.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
