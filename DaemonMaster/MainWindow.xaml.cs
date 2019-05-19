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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DaemonMaster.Core;
using DaemonMaster.Core.Config;
using DaemonMaster.Core.Exceptions;
using DaemonMaster.Core.Jobs;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Core.Win32.PInvoke.Winsta;
using DaemonMaster.Language;
using DaemonMasterService;
using TimeoutException = System.ServiceProcess.TimeoutException;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Config _config;
        private readonly ObservableCollection<ServiceListViewItem> _processCollection;
        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        public MainWindow()
        {
            //Initialize GUI
            InitializeComponent();

            //Get the configuration
            _config = ConfigManagement.GetConfig;


            //Fill the list and subs to the event
            _processCollection = new ObservableCollection<ServiceListViewItem>(RegistryManagement.LoadInstalledServices().ConvertAll(x => new ServiceListViewItem(x.ServiceName, x.DisplayName, x.BinaryPath, Equals(x.Credentials, ServiceCredentials.LocalSystem))));
            _processCollection.CollectionChanged += ProcessCollectionOnCollectionChanged;

            //Start ListView updater
            StartListViewUpdateTimer(_config.UpdateInterval);

            //Show the list in the list view
            ListViewDaemons.ItemsSource = _processCollection;
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
            if (ListViewDaemons.SelectedItem == null)
                return;

            RemoveDaemon((ServiceListViewItem)ListViewDaemons.SelectedItem);
        }

        private void buttonSwitchToSession0_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSession0();
        }

        //ListBox
        private void MenuItem_Start_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            StartService((ServiceListViewItem)ListViewDaemons.SelectedItem);
        }

        private void MenuItem_Stop_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            StopService((ServiceListViewItem)ListViewDaemons.SelectedItem);
        }

        private void MenuItem_Kill_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            KillService((ServiceListViewItem)ListViewDaemons.SelectedItem);
        }

        private void MenuItem_Delete_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            RemoveDaemon((ServiceListViewItem)ListViewDaemons.SelectedItem);
        }

        private void MenuItem_StartInSession_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            var serviceListViewItem = (ServiceListViewItem)ListViewDaemons.SelectedItem;

            try
            {
                //using (var serviceController = new ServiceController(serviceListViewItem.ServiceName))
                //{
                //    var args = new[] { "-startInUserSession" };
                //    serviceController.Start(args);
                //}

                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(serviceListViewItem.ServiceName, Advapi32.ServiceAccessRights.Start))
                    {
                        serviceHandle.Start(new[] { "-startInUserSession" });
                    }
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
            if (ListViewDaemons.SelectedItem == null)
                return;

            //Only show "Start in session" if the service run under the Local System account
            MenuItemStartInSession.IsEnabled = ((ServiceListViewItem)ListViewDaemons.SelectedItem).UseLocalSystem;
        }

        //MENU

        private void MenuItem_AddDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            AddDaemon();
        }

        private void MenuItem_RemoveDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            RemoveDaemon((ServiceListViewItem)ListViewDaemons.SelectedItem);
        }

        private void MenuItem_EditDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            EditDaemon();
        }

        private void MenuItem_RefreshListOnClick(object sender, RoutedEventArgs e)
        {
            //Clear the list and refill it so that the event gets fired
            _processCollection.Clear();

            List<ServiceListViewItem> list = RegistryManagement.LoadInstalledServices().ConvertAll(x => new ServiceListViewItem(x.ServiceName, x.DisplayName, x.BinaryPath, Equals(x.Credentials, ServiceCredentials.LocalSystem)));
            foreach (ServiceListViewItem item in list)
            {
                _processCollection.Add(item);
            }

            //Force Update after refresh
            UpdateListView(null, EventArgs.Empty);
        }

        private void MenuItem_CheckForUpdates_OnClick(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private void MenuItem_Credits_OnClick(object sender, RoutedEventArgs e)
        {
            var creditsWindow = new CreditsWindow();
            creditsWindow.ShowDialog();
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private void UpdateListViewFilter()
        {
            if (string.IsNullOrWhiteSpace(TextBoxFilter.Text))
            {
                if (Equals(ListViewDaemons.ItemsSource, _processCollection) || ListViewDaemons.Items.Count <= 0)
                    return;

                ListViewDaemons.Items.Clear();
                ListViewDaemons.ItemsSource = _processCollection;
            }

            ListViewDaemons.ItemsSource = null;
            ListViewDaemons.Items.Clear();

            foreach (ServiceListViewItem item in _processCollection)
            {
                if (item.DisplayName.ToLower().Contains(TextBoxFilter.Text.ToLower()))
                    ListViewDaemons.Items.Add(item);
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
            if (ListViewDaemons.Items.Count <= 128)
            {
                try
                {
                    var serviceEditWindow = new ServiceEditWindow(null);
                    bool? dialogResult = serviceEditWindow.ShowDialog(); //Wait until the window is closed
                    if (dialogResult.HasValue && dialogResult.Value)
                    {
                        _processCollection.Add(ServiceListViewItem.CreateFromServiceDefinition(serviceEditWindow.GetServiceStartInfo()));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(_resManager.GetString("failed_to_create_a_new_service") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(_resManager.GetString("max_limit_reached"), _resManager.GetString("warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveDaemon(ServiceListViewItem serviceListViewItem)
        {
            MessageBoxResult result = MessageBox.Show(_resManager.GetString("msg_warning_delete"), _resManager.GetString("question"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(serviceListViewItem.ServiceName, Advapi32.ServiceAccessRights.AllAccess))
                    {
                        serviceHandle.DeleteService();
                    }
                }
                _processCollection.Remove(_processCollection.Single(i => i.ServiceName == serviceListViewItem.ServiceName));

                MessageBox.Show(_resManager.GetString("the_service_deletion_was_successful"),
                    _resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ServiceNotStoppedException)
            {
                result = MessageBox.Show(_resManager.GetString("you_must_stop_the_service_first"),
                   _resManager.GetString("information"), MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    StopService(serviceListViewItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("the_service_deletion_was_unsuccessful") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDaemon()
        {
            if (ListViewDaemons.SelectedItem == null)
                return;

            var serviceListViewItem = (ServiceListViewItem)ListViewDaemons.SelectedItem;

            //Stop service first
            using (var serviceController = new ServiceController(serviceListViewItem.ServiceName))
            {
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    MessageBoxResult result = MessageBox.Show(_resManager.GetString("you_must_stop_the_service_first"),
                        _resManager.GetString("information"), MessageBoxButton.YesNo, MessageBoxImage.Information);


                    if (result == MessageBoxResult.Yes)
                    {
                        StopService(serviceListViewItem);

                        try
                        {
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        }
                        catch (TimeoutException ex)
                        {
                            //TODO: Better message
                            MessageBox.Show("Cannot stop the service:\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }


            try
            {
                //Open service edit window with the data from registry
                var serviceEditWindow = new ServiceEditWindow(RegistryManagement.LoadServiceStartInfosFromRegistry(serviceListViewItem.ServiceName));
                //stops until windows has been closed
                bool? dialogResult = serviceEditWindow.ShowDialog();
                //Check result
                if (dialogResult.HasValue && dialogResult.Value)
                {
                    DmServiceDefinition serviceDefinition = serviceEditWindow.GetServiceStartInfo();
                    if (string.Equals(serviceDefinition.ServiceName, serviceListViewItem.ServiceName))
                    {
                        //Update serviceListViewItem
                        _processCollection[_processCollection.IndexOf(serviceListViewItem)] = ServiceListViewItem.CreateFromServiceDefinition(serviceDefinition);
                    }
                    else
                    {
                        //Create new daemon (Import as happend with a diffrent service name => create new service with that name)
                        _processCollection.Add(ServiceListViewItem.CreateFromServiceDefinition(serviceDefinition));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_load_data_from_registry") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartService(ServiceListViewItem serviceListViewItem)
        {
            try
            {
                //using (var serviceController = new ServiceController(serviceListViewItem.ServiceName))
                //{
                //    serviceController.Start();
                //}

                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(serviceListViewItem.ServiceName, Advapi32.ServiceAccessRights.Start))
                    {
                        serviceHandle.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopService(ServiceListViewItem serviceListViewItem)
        {
            try
            {
                //using (var serviceController = new ServiceController(serviceListViewItem.ServiceName))
                //{
                //    serviceController.Stop();
                //}

                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(serviceListViewItem.ServiceName, Advapi32.ServiceAccessRights.Stop))
                    {
                        serviceHandle.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KillService(ServiceListViewItem serviceListViewItem)
        {
            try
            {
                using (var serviceController = new ServiceController(serviceListViewItem.ServiceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                        return;

                    try
                    {
                        serviceController.ExecuteCommand((int)ServiceCommands.ServiceKillProcessAndStop); //Kill command for the process
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(2));
                    }
                    catch (TimeoutException)
                    {
                        if (KillChildProcessJob.IsSupportedWindowsVersion && serviceListViewItem.ServicePid != null)
                        {
                            Process process = Process.GetProcessById((int)serviceListViewItem.ServicePid);
                            process.Kill();
                        }
                        else
                        {
                            MessageBox.Show(_resManager.GetString("cannot_kill_service", CultureInfo.CurrentUICulture), _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_kill_service", CultureInfo.CurrentUICulture) + ":\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SwitchToSession0()
        {
            if (!DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
            {
                MessageBox.Show(_resManager.GetString("windows10_1803_switch_session0", CultureInfo.CurrentUICulture),
                    _resManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }


            if (DaemonMasterUtils.CheckUi0DetectService())
            {
                //if its Windows 10 then showing a warning message
                if (DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
                {
                    MessageBoxResult result =
                        MessageBox.Show(_resManager.GetString("windows10_mouse_keyboard", CultureInfo.CurrentUICulture),
                            _resManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.OKCancel,
                            MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                        return;
                }
                //Switch to session 0
                Winsta.WinStationSwitchToServicesSession();
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
            _ = Updater.Updater.StartAsync("https://github.com/TWC-Software/DaemonMaster");
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
            if (DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
            {
                //If Windows 10 1803 installed don't ask for UI0Detect registry key change
                AskToEnableInteractiveServices();

                if (!DaemonMasterUtils.CheckUi0DetectService())
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
            var guiDispatcherTimer = new DispatcherTimer();
            guiDispatcherTimer.Tick += UpdateListView;
            guiDispatcherTimer.Interval = TimeSpan.FromSeconds(interval);
            guiDispatcherTimer.Start();

            //Force Update on startup
            UpdateListView(null, EventArgs.Empty);
        }

        private void UpdateListView(object sender, EventArgs e)
        {
            foreach (ServiceListViewItem daemonItem in _processCollection)
            {
                daemonItem.UpdateStatus();
            }

            //Force refresh of the listview
            ListViewDaemons.Items.Refresh();
        }
    }
}