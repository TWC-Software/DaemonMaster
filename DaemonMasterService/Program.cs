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

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.ServiceProcess;
using CommandLine;
using DaemonMasterCore;
using Newtonsoft.Json;
using NLog;

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
            Parser.Default.ParseArguments<GeneralOptions, ServiceOptions, InstallOptions>(args)
               .MapResult(
                   (GeneralOptions opts) => RunOptionsAndReturnExitCode(opts),
                   (ServiceOptions opts) => RunServiceAndReturnExitCode(opts),
                   (InstallOptions opts) => RunInstallAndReturnExitCode(opts),
                   errs => 1);
        }

        private static int RunServiceAndReturnExitCode(ServiceOptions opts)
        {
            return StartService(opts.EnablePause);
        }

        private static int RunOptionsAndReturnExitCode(GeneralOptions option)
        {
            int result = 0;

            if (option.KillAllServices)
                if (ServiceManagement.KillAllServices())
                    result = 1;

            if (option.DeleteAllServices)
                if (ServiceManagement.DeleteAllServices())
                    result = 1;

            return result;
        }

        private static int RunInstallAndReturnExitCode(InstallOptions opts)
        {
            if (!String.IsNullOrWhiteSpace(opts.DmdfFile) && File.Exists(opts.DmdfFile))
            {
                return InstallDmdf(opts.DmdfFile, opts.Password.ConvertStringToSecureString());
            }
            else
            {
                throw new NotImplementedException("Comming soon...");
            }
        }

        private static int InstallDmdf(string path, SecureString pw)
        {
            try
            {
                using (StreamReader streamReader = File.OpenText(path))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    ServiceStartInfo serviceStartInfo = (ServiceStartInfo)serializer.Deserialize(streamReader, typeof(ServiceStartInfo));

                    //Check if the service already exist
                    if (ServiceController.GetServices().Any(service => String.Equals(serviceStartInfo.ServiceName, service.ServiceName)))
                        throw new ArgumentException("A service with the same name already exist.");

                    //Check if the password is valid when not the local system account is used
                    if (!serviceStartInfo.UseLocalSystem)
                    {
                        if (pw != null && pw.Length > 0)
                        {
                            serviceStartInfo.Password = pw; //Set password
                        }
                        else
                        {
                            throw new ArgumentException("You must give a password with the --pw parameter when you will use a custom user account.");
                        }
                    }

                    ServiceManagement.CreateInteractiveService(serviceStartInfo);

                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to install the DMDF service:\n" + ex.Message);
                return 1;
            }
        }

        private static int StartService(bool enablePause)
        {
            var servicesToRun = new ServiceBase[]
            {
                new Service(enablePause)
            };

            try
            {
                ServiceBase.Run(servicesToRun);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start the service: \n" + ex.Message);
                return 1;
            }
        }
    }
}
