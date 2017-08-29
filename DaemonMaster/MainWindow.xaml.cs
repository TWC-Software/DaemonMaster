/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: MainWindow 
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


using AutoUpdaterDotNET;
using DaemonMaster.Language;
using DaemonMasterCore;
using DaemonMasterCore.Config;
using DaemonMasterCore.Exceptions;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<DaemonItem> _processCollection = null;
        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        public MainWindow()
        {
            //Load and apply config
            Config config = ConfigManagement.LoadConfig();

            #region Chose language
            //Set the language of the threads
            CultureInfo cultureInfo;
            if (String.IsNullOrWhiteSpace(config.Language) || config.Language == "windows")
            {
                cultureInfo = CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    cultureInfo = new CultureInfo(config.Language);
                }
                catch (Exception)
                {
                    cultureInfo = CultureInfo.CurrentCulture;
                }
            }

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            #endregion

            //Initialize GUI
            InitializeComponent();

            //Fragt, wenn der RegKey nicht gesetzt ist, ob dieser gesetzt werden soll
            if (!AskToEnableInteractiveServices())
                this.Close();

            _processCollection = RegistryManagement.LoadDaemonItemsFromRegistry();
            //Start ListView Updater
            StartListViewUpdateTimer(config.UpdateInterval);

            //Aktualisiert die Liste zum start
            listViewDaemons.ItemsSource = _processCollection;

            if (!ServiceManagement.CheckUI0DetectService())
            {
                MessageBox.Show(_resManager.GetString("error_ui0service", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          GUI ELEMENTS                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GUI_Elements



        //Buttons

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            AddDaemon();
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            EditDaemon();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            RemoveDaemon((DaemonItem)listViewDaemons.SelectedItem);
        }

        private void buttonFilter_Click(object sender, RoutedEventArgs e)
        {

            foreach (DaemonItem d in _processCollection)
            {
                if (d.DisplayName.Contains(textBoxFilter.Text))
                {
                    listViewDaemons.SelectedItem = d;
                    break;
                }
            }
        }

        private void buttonSwitchToSession0_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSession0();
        }

        //ListBox
        private void MenuItem_Start_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            StartService((DaemonItem)listViewDaemons.SelectedItem);
        }

        private void MenuItem_Stop_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            StopService((DaemonItem)listViewDaemons.SelectedItem);
        }

        private void MenuItem_Kill_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            KillService((DaemonItem)listViewDaemons.SelectedItem);
        }

        private void MenuItem_Delete_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            RemoveDaemon((DaemonItem)listViewDaemons.SelectedItem);
        }

        private void listBoxDaemons_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditDaemon();
        }

        //MENU

        private void MenuItem_AddDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            AddDaemon();
        }

        private void MenuItem_RemoveDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            RemoveDaemon((DaemonItem)listViewDaemons.SelectedItem);
        }

        private void MenuItem_EditDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            EditDaemon();
        }

        private void MenuItem_CheckForUpdates_OnClick(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private void MenuItem_Credits_OnClick(object sender, RoutedEventArgs e)
        {
            CreditsWindow creditsWindow = new CreditsWindow();
            creditsWindow.ShowDialog();
        }

        private void MenuItem_Export_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Filter = "DMDF (*.dmdf)|*.dmdf|" +
                         "All files (*.*)|*.*",
                DefaultExt = "dmdf",
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true
            };

            try
            {
                if (saveFileDialog.ShowDialog() == true)
                {
                    DaemonMasterUtils.ExportItem(((DaemonItem)listViewDaemons.SelectedItem).ServiceName, saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_export_daemon") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_Import_OnClick(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Filter = "DMDF (*.dmdf)|*.dmdf|" +
                         "All files (*.*)|*.*",
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                Multiselect = false
            };

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    ImportDaemon(DaemonMasterUtils.ImportItem(openFileDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_import_daemon") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_StartInSession_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            DaemonItem daemonItem = (DaemonItem)listViewDaemons.SelectedItem;

            try
            {
                switch (ServiceManagement.StartService(daemonItem.ServiceName, true))
                {
                    case DaemonServiceState.Unsuccessful:
                        MessageBox.Show(_resManager.GetString("start_was_unsuccessful"),
                            _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    case DaemonServiceState.Successful:
                        MessageBox.Show(_resManager.GetString("start_was_successful"),
                            _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case DaemonServiceState.AlreadyStopped:
                        MessageBox.Show(_resManager.GetString("the_selected_process_is_already_started"),
                            _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_StopInSession_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            DaemonItem daemonItem = (DaemonItem)listViewDaemons.SelectedItem;

            try
            {
                switch (ServiceManagement.StopService(daemonItem.ServiceName))
                {
                    case DaemonServiceState.Unsuccessful:
                        MessageBoxResult result = MessageBox.Show(_resManager.GetString("stop_was_unsuccessful"),
                            _resManager.GetString("error"), MessageBoxButton.YesNo, MessageBoxImage.Error);

                        if (result == MessageBoxResult.Yes)
                            KillService(daemonItem);
                        break;
                    case DaemonServiceState.Successful:
                        MessageBox.Show(_resManager.GetString("stop_was_successful"),
                            _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case DaemonServiceState.AlreadyStopped:
                        MessageBox.Show(_resManager.GetString("the_selected_process_does_not_exist"),
                            _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_KillInSession_OnClick(object sender, RoutedEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            KillService((DaemonItem)listViewDaemons.SelectedItem);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private bool AskToEnableInteractiveServices()
        {
            //Wenn der RegKey nicht gestetzt ist, soll der Nutzer gefragt werden
            if (!RegistryManagement.CheckNoInteractiveServicesRegKey())
            {
                MessageBoxResult result = MessageBox.Show(_resManager.GetString("interactive_service_regkey_not_set"), _resManager.GetString("question"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (RegistryManagement.ActivateInteractiveServices())
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(_resManager.GetString("problem_occurred"), _resManager.GetString("error"), MessageBoxButton.OK);
                    }
                }

                return false;
            }

            return true;
        }

        private void AddDaemon()
        {
            if (listViewDaemons.Items.Count <= 256)
            {
                EditAddWindow addProcessWindow = EditAddWindow.OpenEditAddWindowWithDefaultValues(); // Neues Event Im EditAddWindow Fenster
                var dialogResult = addProcessWindow.ShowDialog(); // Fenster geht auf, Code geht erst weiter wenn Fesnter geschlossen ist
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    _processCollection.Add(addProcessWindow.DaemonItem);
                }
            }
            else
            {
                MessageBox.Show(_resManager.GetString("max_limit_reached"), _resManager.GetString("warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ImportDaemon(Daemon daemon)
        {
            EditAddWindow addProcessWindow = EditAddWindow.OpenEditAddWindowForImporting(daemon); // Neues Event Im EditAddWindow Fenster
            var dialogResult = addProcessWindow.ShowDialog(); // Fenster geht auf, Code geht erst weiter wenn Fesnter geschlossen ist
            if (dialogResult.HasValue && dialogResult.Value)
            {
                _processCollection.Add(addProcessWindow.DaemonItem);
            }
        }

        private void RemoveDaemon(DaemonItem daemonItem)
        {
            try
            {
                ServiceManagement.DeleteService(daemonItem.ServiceName);
                _processCollection.RemoveAt(listViewDaemons.SelectedIndex);

                MessageBox.Show(_resManager.GetString("the_service_deletion_was_successful"),
                    _resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ServiceNotStoppedException)
            {
                MessageBoxResult result = MessageBox.Show(_resManager.GetString("you_must_stop_the_service_first"),
                    _resManager.GetString("information"), MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    StopService(daemonItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("the_service_deletion_was_unsuccessful") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDaemon()
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            DaemonItem daemonItem = (DaemonItem)listViewDaemons.SelectedItem;

            if (ServiceManagement.IsServiceRunning(daemonItem.ServiceName))
            {
                MessageBoxResult result = MessageBox.Show(_resManager.GetString("you_must_stop_the_service_first"),
                    _resManager.GetString("information"), MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    StopService(daemonItem);
                }
                else
                {
                    return;
                }
            }

            EditAddWindow addProcessWindow = EditAddWindow.OpenEditAddWindowForEditing(daemonItem);
            var dialogResult = addProcessWindow.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                _processCollection[_processCollection.IndexOf(addProcessWindow.OldDaemonItem)] = addProcessWindow.DaemonItem;
            }
        }

        private void StartService(DaemonItem daemonItem)
        {
            try
            {
                switch (ServiceManagement.StartService(daemonItem.ServiceName))
                {
                    case DaemonServiceState.Unsuccessful:
                        MessageBox.Show(_resManager.GetString("cannot_start_the_service"), _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        break;

                    case DaemonServiceState.AlreadyStarted:
                        MessageBox.Show(_resManager.GetString("cannot_start_the_service_already_running"), _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case DaemonServiceState.Successful:
                        MessageBox.Show(_resManager.GetString("service_start_was_successful"), _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopService(DaemonItem daemonItem)
        {
            try
            {
                switch (ServiceManagement.StopService(daemonItem.ServiceName))
                {
                    case DaemonServiceState.Unsuccessful:
                        MessageBox.Show(_resManager.GetString("cannot_stop_the_service"), _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        break;

                    case DaemonServiceState.AlreadyStopped:
                        MessageBox.Show(_resManager.GetString("cannot_stop_the_service_already_stopped"), _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case DaemonServiceState.Successful:
                        MessageBox.Show(_resManager.GetString("service_stop_was_successful"), _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KillService(DaemonItem daemonItem)
        {
            try
            {
                switch (ServiceManagement.KillService(daemonItem.ServiceName))
                {
                    case DaemonServiceState.AlreadyStopped:
                        MessageBox.Show(_resManager.GetString("cannot_stop_the_service_already_stopped"),
                            _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        break;

                    case DaemonServiceState.Successful:
                        MessageBox.Show(_resManager.GetString("the_process_killing_was_successful"),
                            _resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case DaemonServiceState.Unsuccessful:
                        MessageBox.Show(_resManager.GetString("the_process_killing_was_unsuccessful"),
                            _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SwitchToSession0()
        {
            if (ServiceManagement.CheckUI0DetectService())
            {
                //if its Windows 10 then showing a warning message
                if (Environment.OSVersion.Version.Major == 10)
                {
                    MessageBoxResult result =
                        MessageBox.Show(_resManager.GetString("windows10_mouse_keyboard", CultureInfo.CurrentUICulture),
                            _resManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.OKCancel,
                            MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                        return;
                }
                //Switch to session 0
                NativeMethods.WinStationSwitchToServicesSession();
            }
            else
            {
                MessageBox.Show(
                    _resManager.GetString("failed_start_UI0detect_service", CultureInfo.CurrentUICulture),
                    _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CheckForUpdates()
        {
            AutoUpdater.CurrentCulture = CultureInfo.CurrentCulture;
            AutoUpdater.ShowSkipButton = true;
            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/TWC-Software/DaemonMaster/master/AutoUpdater.xml", typeof(MainWindow).Assembly);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          EVENT HANDLER                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigManagement.SaveConfig();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        GUI Update Timer                                              //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void StartListViewUpdateTimer(uint interval)
        {
            DispatcherTimer guiDispatcherTimer = new DispatcherTimer();
            guiDispatcherTimer.Tick += UpdateListView;
            guiDispatcherTimer.Interval = TimeSpan.FromSeconds(interval);
            guiDispatcherTimer.Start();

            //Force Update on startup
            UpdateListView(null, EventArgs.Empty);
        }

        private void UpdateListView(object sender, EventArgs e)
        {
            foreach (var daemonItem in _processCollection)
            {
                daemonItem.ServiceState = ServiceManagement.GetServiceStatus(daemonItem.ServiceName);
                daemonItem.PID = ServiceManagement.GetPIDByServiceName(daemonItem.ServiceName);
            }

            //Force refresh of the listview
            listViewDaemons.Items.Refresh();
        }
    }
}