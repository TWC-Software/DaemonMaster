/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: Program
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

using DaemonMasterCore;
using NLog;
using System.ServiceProcess;

namespace DaemonMasterService
{
    static class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length <= 0)
                return;

            switch (args[0])
            {
                case "-console":
                    break;

                case "-service":
                    if (args.Length > 1 && args[1] == "-enablePause")
                    {
                        StartService(true);
                    }
                    else
                    {
                        StartService(false);
                    }
                    break;

                case "-deleteAllServices":
                    _logger.Info("Delete services...");
                    ServiceManagement.DeleteAllServices();
                    _logger.Info("Success!");
                    break;

                case "-killAllServices":
                    _logger.Info("Killing services...");
                    ServiceManagement.KillAllServices();
                    _logger.Info("Success!");
                    break;
            }
        }

        private static void StartService(bool enablePause)
        {

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service(enablePause)
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
