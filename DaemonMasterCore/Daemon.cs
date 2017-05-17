/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: DAEMON CONFIG FILE
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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////


using System;
using Newtonsoft.Json;
using System.Windows.Media;

namespace DaemonMasterCore
{
    public class Daemon
    {
        //Default parameter
        public Daemon()
        {
            Parameter = String.Empty;
            UserName = String.Empty;
            UserPassword = String.Empty;

            MaxRestarts = 3;
            ProcessKillTime = 9500;
            ProcessRestartDelay = 2500;
            CounterResetTime = 10000;

            ConsoleApplication = false;
            UseCtrlC = false;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public string FileDir { get; set; }
        public string FileName { get; set; }
        public string FullPath => FileDir + @"\" + FileName;

        public string Parameter { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }

        public int MaxRestarts { get; set; }
        public int ProcessKillTime { get; set; }
        public int ProcessRestartDelay { get; set; }
        public int CounterResetTime { get; set; }

        public bool ConsoleApplication { get; set; }
        public bool UseCtrlC { get; set; }
    }
}
