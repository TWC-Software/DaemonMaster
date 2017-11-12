/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: PluginContracts
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

namespace DaemonMasterCore.PluginSystem
{
    public class PluginContracts
    {
        public interface IPlugin
        {
            string PluginName { get; }
            string PluginVersion { get; }
            string ApiVersion { get; }
            string Author { get; }

            void Init(IAPI api);
            void UnLoad();
        }
    }
}
