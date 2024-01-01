/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: DownloadProgressWindow
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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using DaemonMaster.Updater.Resources;

namespace DaemonMaster.Updater
{
    /// <summary>
    /// Interaktionslogik für DownloadProgressWindow.xaml
    /// </summary>
    public partial class DownloadProgressWindow : Window
    {
        private readonly Uri _downloadUrl;
        private readonly WebProxy _webProxy;
        private WebClient _webClient;

        private string _tempFile;


        public DownloadProgressWindow(Uri downloadUrl, WebProxy webProxy)
        {
            InitializeComponent();

            _downloadUrl = downloadUrl;
            _webProxy = webProxy;
        }

        private void DownloadProgressWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _ = StartDownloadFileAsync();
        }

        /// <summary>
        /// Start the download of the file
        /// </summary>
        /// <returns></returns>
        private async Task StartDownloadFileAsync()
        {
            try
            {
                _webClient = new WebClient();
                _webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                _webClient.Headers.Add("user-agent", "DaemonMaster");

                //Disable SSLv3
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Ssl3;

                if (_webProxy != null)
                {
                    _webClient.Proxy = _webProxy;
                }

                _webClient.DownloadProgressChanged += WebClientOnDownloadProgressChanged;
                _webClient.DownloadFileCompleted += WebClientOnDownloadFileCompleted;

                _tempFile = Path.GetTempPath() + Guid.NewGuid() + Path.GetExtension(_downloadUrl.AbsolutePath);

                await _webClient.DownloadFileTaskAsync(_downloadUrl, _tempFile);
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
            {
                // ignore this exception (throwed when CancelAsync has been called)
            }
            catch (Exception ex)
            {
                RemoveFile(_tempFile);
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _webClient?.Dispose();
                _webClient = null;
            }
        }

        /// <summary>
        /// Runs when the donwload ist completed or cancelled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="asyncCompletedEventArgs"></param>
        private void WebClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs asyncCompletedEventArgs)
        {
            try
            {
                if (!asyncCompletedEventArgs.Cancelled)
                {
                    if (asyncCompletedEventArgs.Error != null)
                    {
                        MessageBox.Show(asyncCompletedEventArgs.Error.Message, asyncCompletedEventArgs.Error.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);

                    }

                    StartUpdateFile(_tempFile);
                    return;
                }

                RemoveFile(_tempFile);
            }
            catch (Exception ex)
            {
                RemoveFile(_tempFile);
                MessageBox.Show(ex.Message, ex.GetType().ToString());
            }
        }

        /// <summary>
        /// Update the percentage in the download GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="downloadProgressChangedEventArgs"></param>
        private void WebClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs downloadProgressChangedEventArgs)
        {
            DownloadProgressBar.Value = downloadProgressChangedEventArgs.ProgressPercentage;
            LabelPercent.Content = downloadProgressChangedEventArgs.ProgressPercentage + "%";
        }


        /// <summary>
        /// Start the given File
        /// </summary>
        private void StartUpdateFile(string file)
        {
            if (File.Exists(file))
            {
                var startInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = file,
                };
                var process = new Process()
                {
                    StartInfo = startInfo,
                };

                DialogResult = process.Start();
            }
            else
            {
                MessageBox.Show(downloadProgressWindowLang.msg_file_not_found, downloadProgressWindowLang.msg_titel_error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Remove the file
        /// </summary>
        private void RemoveFile(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        /// <summary>
        /// Cancel the current download operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_webClient != null)
            {
                _webClient.CancelAsync();
                _webClient.Dispose();
                _webClient = null;
            }

            DialogResult = false;
        }
    }
}