/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: MAIN GUI 
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
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;
using DaemonMaster.Config;
using DaemonMaster.Core;
using DaemonMaster.Language;
using DaemonMaster.Updater.Persistence;
using DaemonMaster.Views;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        internal const string DaemonMasterAppRegPath = @"SOFTWARE\TWC-Software\DaemonMaster";
        internal const string EventLogSource = "DaemonMaster";

        private static readonly ResourceManager ResManager = new ResourceManager(typeof(lang));

        App()
        {
            InitializeComponent();
        }

        [STAThread]
        public static void Main()
        {
            CreateAndCheckEventLogSource();

            #region Config
            var config = ConfigManagement.LoadConfig();
            ConfigManagement.SaveConfig(); //Create one with default loaded values
            #endregion

            #region Chose language
            //Set the language of the threads
            CultureInfo cultureInfo;
            if (string.IsNullOrWhiteSpace(config.Language) || config.Language == "windows")
            {
                cultureInfo = CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    cultureInfo = new CultureInfo(config.Language);
                }
                catch
                {
                    cultureInfo = CultureInfo.CurrentCulture;
                }
            }

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            #endregion

            #region AutoUpdater
            Updater.Updater.PersistenceProvider = new RegistryMachinePersistenceProvider(DaemonMasterAppRegPath);

            if (!ConfigManagement.GetConfig.DisableCheckForUpdates)
                _ = Updater.Updater.StartAsync("https://github.com/TWC-Software/DaemonMaster");
            #endregion

            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                             MAINWINDOW                                               //
            //////////////////////////////////////////////////////////////////////////////////////////////////////////

            var app = new App();
            var mainWindow = new MainWindow();
            app.Run(mainWindow);
        }

        private static void CreateAndCheckEventLogSource()
        {
            try
            {
                EventLogManager.CheckSourceExists();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
