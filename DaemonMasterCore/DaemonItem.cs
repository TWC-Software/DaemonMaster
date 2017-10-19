/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: DaemonItem
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


using System.ServiceProcess;
using System.Windows.Media;

namespace DaemonMasterCore
{
    public class DaemonItem
    {
        private ImageSource _icon = null;
        private string _fullPath = null;

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public ServiceControllerStatus ServiceState { get; set; }
        public bool UseLocalSystem { get; set; }

        public uint PID { get; set; }

        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                //Get the new Icon
                _icon = DaemonMasterUtils.GetIcon(value);
            }
        }

        public ImageSource Icon => _icon;
    }
}
