using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("StructMarshaling", "IDE0032:UseAutoProperty", Justification = "Auto properties should not be used for marshalling (explicit struct layout not guaranteed).")]
        [SuppressMessage("ReSharper", "ConvertToAutoProperty", Justification = "Auto properties should not be used for marshalling (explicit struct layout not guaranteed).")]
        public struct ScAction
        {
            private ScActionType actionType;
            private uint delay;

            /// <summary>
            /// Initializes a new instance of the <see cref="ScAction"/> struct.
            /// </summary>
            /// <param name="actionType">The action that should be performed.</param>
            /// <param name="delay">The time to wait before starting the specified action (in ms).</param>
            public ScAction(ScActionType actionType, TimeSpan delay)
            {
                this.actionType = actionType;
                this.delay = (uint) Math.Round(delay.TotalMilliseconds);
            }

            /// <summary>
            /// The time to wait before starting the specified action (in ms).
            /// </summary>
            public TimeSpan Delay
            {
                get => TimeSpan.FromMilliseconds(delay);
                set => delay = (uint) Math.Round(value.TotalMilliseconds);
            }

            /// <summary>
            /// The action that should be performed.
            /// </summary>
            public ScActionType ActionType
            {
                get => actionType;
                set => actionType = value;
            }
        }
    }
}
