/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: IAPI
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


using System.Collections.ObjectModel;

namespace DaemonMasterCore.PluginSystem
{
    public delegate void OnServiceAddedEventHandler(string serviceName);
    public delegate void OnServiceDeletedEventHandler(string serviceName);

    public interface IAPI
    {
        event OnServiceAddedEventHandler OnServiceAddedEvent;
        event OnServiceDeletedEventHandler OnServiceDeletedEvent;

        ObservableCollection<ServiceListViewItem> DaemonItems { get; }
    }
}
