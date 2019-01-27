using System;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [Flags]
        public enum CreationFlags : uint
        {
            DebugProcess = 0x00000001,
            DebugOnlyThisProcess = 0x00000002,
            CreateSuspended = 0x00000004,
            DetachedProcess = 0x00000008,
            CreateNewConsole = 0x00000010,
            NormalPriorityClass = 0x00000020,
            IdlePriorityClass = 0x00000040,
            HighPriorityClass = 0x00000080,
            RealtimePriorityClass = 0x00000100,
            CreateNewProcessGroup = 0x00000200,
            CreateUnicodeEnvironment = 0x00000400,
            CreateSeparateWowVdm = 0x00000800,
            CreateSharedWowVdm = 0x00001000,
            CreateForcedos = 0x00002000,
            BelowNormalPriorityClass = 0x00004000,
            AboveNormalPriorityClass = 0x00008000,
            InheritParentAffinity = 0x00010000,
            InheritCallerPriority = 0x00020000,
            CreateProtectedProcess = 0x00040000,
            ExtendedStartupinfoPresent = 0x00080000,
            ProcessModeBackgroundBegin = 0x00100000,
            ProcessModeBackgroundEnd = 0x00200000,
            CreateBreakawayFromJob = 0x01000000,
            CreatePreserveCodeAuthzLevel = 0x02000000,
            CreateDefaultErrorMode = 0x04000000,
            CreateNoWindow = 0x08000000,
            ProfileUser = 0x10000000,
            ProfileKernel = 0x20000000,
            ProfileServer = 0x40000000,
            CreateIgnoreSystemDefault = 0x80000000,
        }
    }
}
