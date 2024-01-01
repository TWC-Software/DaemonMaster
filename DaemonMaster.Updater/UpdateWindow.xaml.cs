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

using DaemonMaster.Updater.Resources;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace DaemonMaster.Updater
{
    public partial class UpdateWindow : Window
    {
        static readonly Uri SomeBaseUri = new Uri("http://canbeanything");

        public UpdateWindow()
        {
            InitializeComponent();

            LabelUpdate.Content = string.Format(updaterWindowLang.label_update, Updater.AppName);
            LabelDescription.Content = string.Format(updaterWindowLang.label_description, Updater.AppName, Updater.LastGitHubRelease.Version);
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = OpenDownloadProgressWindow();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonSkip_Click(object sender, RoutedEventArgs e)
        {
            Updater.PersistenceProvider.SetSkippedVersion(Updater.LastGitHubRelease.Version);
            DialogResult = false;
        }

        private bool OpenDownloadProgressWindow()
        {
            try
            {
                string downloadUrl = Updater.LastGitHubRelease.Assets.First(x => GetFileNameFromUrl(x.FileUrl).Contains(Updater.AppName + "_Setup_" + Updater.LastGitHubRelease.Version)).FileUrl;
                if (string.IsNullOrWhiteSpace(downloadUrl))
                    return false;

                var downloadWindow = new DownloadProgressWindow(new Uri(downloadUrl), null);
                downloadWindow.ShowDialog();

                return downloadWindow.DialogResult.HasValue && downloadWindow.DialogResult.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, updaterWindowLang.error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        static string GetFileNameFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                uri = new Uri(SomeBaseUri, url);

            return Path.GetFileName(uri.LocalPath);
        }
    }
}
