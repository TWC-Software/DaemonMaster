/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ShellLinkWrapper
//  
//  This file is part of DeamonMaster.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable InconsistentNaming

namespace DaemonMaster.Core.Win32
{
    public class ShellLinkWrapper : IDisposable
    {
        private bool _isDisposed;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             PINVOKE                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region From http://pinvoke.net and https://stackoverflow.com/questions/139010/how-to-resolve-a-lnk-in-c-sharp and me ;)       

        private IShellLinkW shellLink;

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath,
                out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);

            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetIDList(out IntPtr ppidl);

            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetIDList(IntPtr pidl);

            /// <summary>Retrieves the description string for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            /// <summary>Retrieves the hot key for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetHotkey(out short pwHotkey);

            /// <summary>Sets a hot key for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetHotkey(short wHotkey);

            /// <summary>Retrieves the show command for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetShowCmd(out int piShowCmd);

            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetShowCmd(int iShowCmd);

            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath, out int piIcon);

            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            /// <summary>Sets the relative path to the Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int Resolve(IntPtr hwnd, SLR_FLAGS fFlags);

            /// <summary>Sets the path and file name of a Shell link object</summary>
            [return: MarshalAs(UnmanagedType.I4)]
            int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("0000010c-0000-0000-c000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPersist
        {
            [PreserveSig]
            void GetClassID(out Guid pClassID);
        }

        [Flags]
        enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,

            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,

            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,

            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,

            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,

            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,

            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,

            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }

        [Flags]
        enum SLGP_FLAGS
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SLGP_SHORTPATH = 0x1,

            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SLGP_UNCPRIORITY = 0x2,

            /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
            SLGP_RAWPATH = 0x4
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WIN32_FIND_DATAW
        {
            uint dwFileAttributes;
            long ftCreationTime;
            long ftLastAccessTime;
            long ftLastWriteTime;
            uint nFileSizeHigh;
            uint nFileSizeLow;
            uint dwReserved0;
            uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] string cAlternateFileName;
        }
        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            CONSTANTS                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private const int STGM_READ = 0;
        private const int MAX_PATH = 260;
        private const int INFOTIPSIZE = 1024;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                           CONSTRUCTOR                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ShellLinkWrapper(string filePath)
        {
            try
            {
                shellLink = new ShellLink() as IShellLinkW;
            }
            catch (Exception)
            {
                throw new COMException("Failed to create shellLink object!");
            }

            //Load shortcut
            LoadShortcut(filePath);
        }

        private IPersistFile PersistFile
        {
            get
            {
                var persistFile = shellLink as IPersistFile;

                if (persistFile == null)
                    throw new COMException("Failed to create IPersistFile from shellLink!");

                return persistFile;
            }
        }

        private void LoadShortcut(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is not valid!");

            if (!File.Exists(filePath))
                throw new ArgumentException("File is not found! ( " + filePath + " )");

            PersistFile.Load(filePath, STGM_READ);
        }

        public void SaveShortcut()
        {
            string shortcutPath = ShortcutPath;

            if (shortcutPath == null)
                throw new ArgumentException("File path is not valid.");

            PersistFile.Save(ShortcutPath, true);
        }

        public void SaveShortcut(string shortcutPath)
        {
            if (shortcutPath == null)
                throw new ArgumentException("File path is not valid.");

            PersistFile.Save(ShortcutPath, true);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            PROPERTY'S                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public string ShortcutPath
        {
            get
            {

                PersistFile.GetCurFile(out string shortcutPath);
                return shortcutPath;
            }
        }

        public string FilePath
        {
            get
            {
                var stringBuilder = new StringBuilder(MAX_PATH);
                var data = new WIN32_FIND_DATAW();

                CheckResult(shellLink.GetPath(stringBuilder, stringBuilder.Capacity, out data, 0));
                return stringBuilder.ToString();
            }

            set
            {
                CheckResult(shellLink.SetPath(value));
            }
        }

        public string WorkingDir
        {
            get
            {
                var stringBuilder = new StringBuilder(MAX_PATH);

                CheckResult(shellLink.GetWorkingDirectory(stringBuilder, stringBuilder.Capacity));
                return stringBuilder.ToString();
            }

            set
            {
                CheckResult(shellLink.SetWorkingDirectory(value));
            }
        }

        public string Arguments
        {
            get
            {
                var stringBuilder = new StringBuilder(INFOTIPSIZE);

                CheckResult(shellLink.GetArguments(stringBuilder, stringBuilder.Capacity));
                return stringBuilder.ToString();
            }

            set
            {
                CheckResult(shellLink.SetArguments(value));
            }
        }

        public string Description
        {
            get
            {
                var stringBuilder = new StringBuilder(INFOTIPSIZE);

                CheckResult(shellLink.GetDescription(stringBuilder, stringBuilder.Capacity));
                return stringBuilder.ToString();
            }

            set
            {
                CheckResult(shellLink.SetDescription(value));
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsShortcut(string shortcutFullPath)
        {
            return string.Equals(Path.GetExtension(shortcutFullPath), ".lnk", StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckResult(int hresult)
        {
            if (hresult != 0)
                throw new Win32Exception(Marshal.GetHRForLastWin32Error());
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            DISPOSE                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Dispose

        ~ShellLinkWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                //Free managed objects here
            }
            //Free unmanaged objects here

            if (shellLink != null)
            {
                // Release all references.
                Marshal.FinalReleaseComObject(shellLink);
                shellLink = null;
            }
            _isDisposed = true;
        }
        #endregion
    }
}
