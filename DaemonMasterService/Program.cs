/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: MAIN SERVICE CLASS 
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


using System.ServiceProcess;

namespace DaemonMasterService
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        //static void Main()
        //{
        //    StartService();
        //}

        static void Main(string[] args)
        {
            if (args.Length <= 0)
                return;

            switch (args[0])
            {
                case "-console":
                    break;

                case "-service":
                    StartService(args[1]);
                    break;
            }
        }

        private static void StartService(string serviceName)
        {

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1(serviceName)
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
