/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SERVICE CODE 
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


using DaemonMasterCore;
using System;
using System.ServiceProcess;

namespace DaemonMasterService
{
    public partial class Service1 : ServiceBase
    {
        private DaemonProcess _processManagement = null;

        public Service1(bool enablePause)
        {
            InitializeComponent();

            CanPauseAndContinue = enablePause;
            CanHandlePowerEvent = false;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            try
            {
                //Load config from registry
                Daemon daemon = RegistryManagement.LoadDaemonFromRegistry(DaemonMasterUtils.GetServiceName());
                _processManagement = new DaemonProcess(daemon);
                _processManagement.StartProcess();
            }
            catch (Exception)
            {
                Stop();
            }
        }

        protected override void OnStop()
        {
            //Stop the process
            _processManagement.StopProcess();
            base.OnStop();
        }

        protected override void OnPause()
        {
            _processManagement.PauseProcess();

            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            _processManagement.ResumeProcess();
        }
    }
}
