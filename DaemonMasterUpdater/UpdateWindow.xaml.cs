/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: UpdateWindow
//  
//  This file is part of DeamonMasterUpdater.
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
using System.Linq;
using System.Resources;
using System.Windows;
using DaemonMasterUpdater.Lang;

namespace DaemonMasterUpdater
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private readonly ResourceManager _resManager = new ResourceManager(typeof(updaterWindowLang));

        public UpdateWindow()
        {
            InitializeComponent();

            LabelUpdate.Content = String.Format(_resManager.GetString("label_update", CultureInfo.CurrentUICulture), Updater.AppName);
            LabelDescription.Content = String.Format(_resManager.GetString("label_description", CultureInfo.CurrentUICulture), Updater.AppName, Updater.LastGitHubRelease.Version);
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = OpenDownloadProgressWindow();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool OpenDownloadProgressWindow()
        {
            var downloadWindow = new DownloadProgressWindow(new Uri(Updater.LastGitHubRelease.Assets.First().FileUrl), null);
            downloadWindow.ShowDialog();

            return downloadWindow.DialogResult.HasValue && downloadWindow.DialogResult.Value;
        }

    }
}
