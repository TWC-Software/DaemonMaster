/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceStartInfo
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
using System.IO;
using System.Security;
using DaemonMasterCore.Win32.PInvoke;
using Newtonsoft.Json;

namespace DaemonMasterCore
{
    public sealed class ServiceStartInfo
    {
        public override string ToString()
        {
            return DisplayName;
        }

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; } = String.Empty;
        public string[] DependOnService { get; set; } = Array.Empty<string>();
        public string[] DependOnGroup { get; set; } = Array.Empty<string>();
        public NativeMethods.SERVICE_START StartType { get; set; } = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
        public bool DelayedStart { get; set; } = false;
        public bool CanInteractWithDesktop { get; set; } = false;

        public string FileDir { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }

        public string FullPath
        {
            set
            {
                FileName = Path.GetFileName(value);
                FileDir = Path.GetDirectoryName(value);
                FileExtension = Path.GetExtension(value);
            }
            get { return FileDir + @"\" + FileName; }
        }

        public string Parameter { get; set; } = String.Empty;
        public bool UseLocalSystem { get; set; } = true;
        public string Username { get; set; } = null;

        [JsonIgnore] public SecureString Password { get; set; } = null;

        public int MaxRestarts { get; set; } = 3;
        public int ProcessKillTime { get; set; } = 9500;
        public int ProcessRestartDelay { get; set; } = 2000;
        public int CounterResetTime { get; set; } = 43200; //12h

        public bool ConsoleApplication { get; set; } = false;
        public bool UseCtrlC { get; set; } = false;
    }
}
