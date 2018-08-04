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
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DaemonMaster.Language;
using DaemonMasterCore;
using DaemonMasterCore.Config;
using DaemonMasterCore.Exceptions;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Config _config;

        private readonly ObservableCollection<DaemonItem> _processCollection;
        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        public MainWindow()
        {
            //Load and apply config
            _config = ConfigManagement.LoadConfig();

            #region Chose language

            //Set the language of the threads
            CultureInfo cultureInfo;
            if (String.IsNullOrWhiteSpace(_config.Language) || _config.Language == "windows")
            {
                cultureInfo = CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    cultureInfo = new CultureInfo(_config.Language);
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


            _processCollection = RegistryManagement.LoadDaemonItemsFromRegistry();
            _processCollection.CollectionChanged += ProcessCollectionOnCollectionChanged;
            //Start ListView updater
            StartListViewUpdateTimer(_config.UpdateInterval);

            //Update the list at the beginning
            listViewDaemons.ItemsSource = _processCollection;
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

        private void listBoxDaemons_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditDaemon();
        }

        private void ListViewDaemons_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (listViewDaemons.SelectedItem == null)
                return;

            //Only show "Start in session" if the service run under the Local System account
            MenuItem_StartInSession.IsEnabled = ((DaemonItem)listViewDaemons.SelectedItem).UseLocalSystem;
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
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private void UpdateListViewFilter()
        {
            if (String.IsNullOrWhiteSpace(textBoxFilter.Text))
            {
                if (Equals(listViewDaemons.ItemsSource, _processCollection) || listViewDaemons.Items.Count <= 0)
                    return;

                listViewDaemons.Items.Clear();
                listViewDaemons.ItemsSource = _processCollection;
            }

            listViewDaemons.ItemsSource = null;
            listViewDaemons.Items.Clear();

            foreach (var item in _processCollection)
            {
                if (item.DisplayName.ToLower().Contains(textBoxFilter.Text.ToLower()))
                    listViewDaemons.Items.Add(item);
            }
        }

        private bool AskToEnableInteractiveServices()
        {
            try
            {
                //Wenn der RegKey nicht gestetzt ist, soll der Nutzer gefragt werden
                if (!RegistryManagement.CheckNoInteractiveServicesRegKey())
                {
                    MessageBoxResult result = MessageBox.Show(_resManager.GetString("interactive_service_regkey_not_set"), _resManager.GetString("question"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (!RegistryManagement.EnableInteractiveServices(true))
                        {
                            MessageBox.Show(_resManager.GetString("problem_occurred"), _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("failed_to_set_interServ") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void AddDaemon()
        {
            if (listViewDaemons.Items.Count <= 256)
            {
                try
                {
                    EditAddWindow addProcessWindow = EditAddWindow.OpenEditAddWindowWithDefaultValues(); // Neues Event Im EditAddWindow Fenster
                    var dialogResult = addProcessWindow.ShowDialog(); // Fenster geht auf, Code geht erst weiter wenn Fenster geschlossen ist
                    if (dialogResult.HasValue && dialogResult.Value)
                    {
                        _processCollection.Add(addProcessWindow.DaemonItem);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(_resManager.GetString("cannot_load_data_from_registry") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(_resManager.GetString("max_limit_reached"), _resManager.GetString("warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ImportDaemon(Daemon daemon)
        {
            try
            {
                EditAddWindow addProcessWindow = EditAddWindow.OpenEditAddWindowForImporting(daemon); // Neues Event Im EditAddWindow Fenster
                var dialogResult = addProcessWindow.ShowDialog(); // Fenster geht auf, Code geht erst weiter wenn Fesnter geschlossen ist
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    _processCollection.Add(addProcessWindow.DaemonItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_load_data_from_registry") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveDaemon(DaemonItem daemonItem)
        {
            MessageBoxResult result = MessageBox.Show(_resManager.GetString("msg_warning_delete"), _resManager.GetString("question"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                ServiceManagement.DeleteService(daemonItem.ServiceName);
                _processCollection.Remove(_processCollection.Single(i => i.ServiceName == daemonItem.ServiceName));

                MessageBox.Show(_resManager.GetString("the_service_deletion_was_successful"),
                    _resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ServiceNotStoppedException)
            {
                result = MessageBox.Show(_resManager.GetString("you_must_stop_the_service_first"),
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

            try
            {
                EditAddWindow addProcessWindow = EditAddWindow.OpenEditAddWindowForEditing(daemonItem);
                var dialogResult = addProcessWindow.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    _processCollection[_processCollection.IndexOf(addProcessWindow.OldDaemonItem)] = addProcessWindow.DaemonItem;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_load_data_from_registry") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 17134)
            {
                MessageBox.Show(_resManager.GetString("windows10_1803_switch_session0", CultureInfo.CurrentUICulture),
                    _resManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }


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
            _ = DaemonMasterUpdater.Updater.StartAsync("https://github.com/TWC-Software/DaemonMaster");
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          EVENT HANDLER                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConfigManagement.SaveConfig();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //If Windows 10 1803 installed don't ask for start the UI0Detect service
            if (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build < 17134)
            {
                //If Windows 10 1803 installed don't ask for UI0Detect registry key change
                AskToEnableInteractiveServices();

                if (!ServiceManagement.CheckUI0DetectService())
                {
                    MessageBox.Show(_resManager.GetString("error_ui0service", CultureInfo.CurrentUICulture),
                        _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            CheckForUpdates();
        }

        private void ProcessCollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            UpdateListViewFilter();

            //different kind of changes that may have occurred in collection
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
            {
                //your code
            }
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Replace)
            {
                //your code
            }
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Remove)
            {
                //your code
            }
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Move)
            {
                //your code
            }
        }

        private void TextBoxFilter_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateListViewFilter();
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
                daemonItem.UpdateStatus();
            }

            //Force refresh of the listview
            listViewDaemons.Items.Refresh();
        }
    }
}