using System;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32
{
    //FROM PINVOKE
    public static partial class NativeMethods
    {
        [DllImport(DLLFiles.USER32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const Int32 _SYSCOMMAND = 0x0112;

        public enum wParam : Int32
        {
            SC_CLOSE = 0xF060,
            SC_CONTEXTHELP = 0xF180,
            SC_DEFAULT = 0xF160,
            SC_HOTKEY = 0xF150,
            SC_HSCROLL = 0xF080,
            SCF_ISSECURE = 0x00000001,
            SC_KEYMENU = 0xF100,
            SC_MAXIMIZE = 0xF030,
            SC_MINIMIZE = 0xF020,
            SC_MONITORPOWER = 0xF170,
            SC_MOUSEMENU = 0xF090,
            SC_MOVE = 0xF010,
            SC_NEXTWINDOW = 0xF040,
            SC_PREVWINDOW = 0xF050,
            SC_RESTORE = 0xF120,
            SC_SCREENSAVE = 0xF140,
            SC_SIZE = 0xF000,
            SC_TASKLIST = 0xF130,
            SC_VSCROLL = 0xF070
        }

    }
}
