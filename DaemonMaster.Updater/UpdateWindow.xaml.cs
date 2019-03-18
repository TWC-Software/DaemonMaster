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
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows;
using DaemonMaster.Updater.Lang;

namespace DaemonMaster.Updater
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private readonly ResourceManager _resManager = new ResourceManager(typeof(updaterWindowLang));
        static readonly Uri SomeBaseUri = new Uri("http://canbeanything");

        public UpdateWindow()
        {
            InitializeComponent();

            LabelUpdate.Content = string.Format(_resManager.GetString("label_update", CultureInfo.CurrentUICulture), Updater.AppName);
            LabelDescription.Content = string.Format(_resManager.GetString("label_description", CultureInfo.CurrentUICulture), Updater.AppName, Updater.LastGitHubRelease.Version);
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
                MessageBox.Show(ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
