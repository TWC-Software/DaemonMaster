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


using DaemonMaster.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DaemonMaster
{
    public class Daemon
    {
        [JsonConstructor]
        public Daemon()
        {
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public string FileDir { get; set; }
        public string FileName { get; set; }
        public string FullPath
        {
            get { return FileDir + @"\" + FileName; }
        }

        public string Parameter { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public int MaxRestarts { get; set; }

        public int ProcessKillTime { get; set; }
        public int ProcessRestartDelay { get; set; }

        public bool ConsoleApplication { get; set; }
        public bool UseCtrlC { get; set; }

        [JsonIgnore]
        public ImageSource Icon
        {
            get { return DaemonMasterCore.GetIcon(FullPath); }
        }
    }
}
