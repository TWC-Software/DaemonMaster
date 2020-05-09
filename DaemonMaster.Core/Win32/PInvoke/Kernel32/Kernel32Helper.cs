using System;
using System.Diagnostics;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static class Kernel32Helper
    {
        /// <summary>
        /// Converts a ProcessPriorityClass entry to the specific creation flag.
        /// </summary>
        /// <param name="processPriority">The process priority.</param>
        /// <returns></returns>
        public static Kernel32.CreationFlags ConvertToCreationFlag(this ProcessPriorityClass processPriority)
        {
            return processPriority switch
            {
                ProcessPriorityClass.Idle => Kernel32.CreationFlags.IdlePriorityClass,
                ProcessPriorityClass.High => Kernel32.CreationFlags.HighPriorityClass,
                ProcessPriorityClass.RealTime => Kernel32.CreationFlags.RealtimePriorityClass,
                ProcessPriorityClass.BelowNormal => Kernel32.CreationFlags.BelowNormalPriorityClass,
                ProcessPriorityClass.AboveNormal => Kernel32.CreationFlags.AboveNormalPriorityClass,
                ProcessPriorityClass.Normal => Kernel32.CreationFlags.NormalPriorityClass,
                _ => throw new ArgumentOutOfRangeException(nameof(processPriority), processPriority, null)
            };
        }
    }
}
