using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DaemonMaster.Core.Win32.PInvoke.Userenv
{
    public static class Userenv
    {
        private const string DllName = "userenv.dll";

        [DllImport(DllName, SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(ref IntPtr environment, SafeAccessTokenHandle token, bool inherit);

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr environment);
    }
}
