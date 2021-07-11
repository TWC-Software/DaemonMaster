using CommonServiceLocator;
using DaemonMaster.Core;
using DaemonMaster.Core.Jobs;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Core.Win32.PInvoke.Winsta;
using DaemonMaster.Language;
using DaemonMaster.Models;
using DaemonMaster.Utilities;
using DaemonMaster.Utilities.Messages;
using DaemonMaster.Utilities.Services;
using DaemonMasterService;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ConfigManagement = DaemonMaster.Config.ConfigManagement;
using RelayCommand = GalaSoft.MvvmLight.Command.RelayCommand;
using TimeoutException = System.TimeoutException;

namespace DaemonMaster.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        #region Private Members

        private static readonly ResourceManager ResManager = new ResourceManager(typeof(lang));
        private readonly ObservableCollectionEx<ServiceListViewItem> _serviceList;
        #endregion


        #region Public Properties

        private IMessageBoxService _messageBoxService;
        public IMessageBoxService MessageBoxService => _messageBoxService ??= ServiceLocator.Current.GetInstance<IMessageBoxService>();

        private IEditWindowService _editWindowService;
        public IEditWindowService EditWindowService => _editWindowService ??= ServiceLocator.Current.GetInstance<IEditWindowService>();

        private CommandBindingCollection _commands;
        public CommandBindingCollection Commands
        {
            get
            {
                return _commands ??= new CommandBindingCollection
                {
                    new CommandBinding(ApplicationCommands.New, NewExecuted, CanExecutedNewService),
                    new CommandBinding(ApplicationCommands.Delete, DeleteExecuted, CanExecutedDelete),
                    new CommandBinding(ApplicationCommands.Open, OpenExecuted, CanExecutedOpen)
                };
            }
        }

        private ServiceListViewItem _selectedService;
        public ServiceListViewItem SelectedService
        {
            get => _selectedService;
            set => Set(ref _selectedService, value);
        }

        private string _search;
        public string Search
        {
            get => _search;
            set
            {
                Set(ref _search, value);
                ServiceView.Refresh(); // required    
            }
        }


        public ICollectionView ServiceView { get; }

        private ICommand _onLoadedCommand;
        public ICommand OnLoadedCommand => _onLoadedCommand ??= new RelayCommand(OnLoadedExecute);

        private ICommand _refreshListView;
        public ICommand RefreshListView => _refreshListView ??= new RelayCommand((() => {_ = UpdateItemsAsync(); }));

        private ICommand _switchToServiceSessionCommand;
        public ICommand SwitchToServiceSessionCommand => _switchToServiceSessionCommand ??= new RelayCommand(SwitchToServiceSessionExecute, CanExecutedSwitchToServiceSession);

        private ICommand _startServiceCommand;
        public ICommand StartServiceCommand => _startServiceCommand ??= new RelayCommand<bool>(StartServiceExecute, CanExecuteStartService);

        private ICommand _stopServiceCommand;
        public ICommand StopServiceCommand => _stopServiceCommand ??= new RelayCommand(StopServiceExecute, CanExecutedStopService);

        private ICommand _restartServiceCommand;
        public ICommand RestartServiceCommand => _restartServiceCommand ??= new RelayCommand(RestartServiceExecute, CanExecutedRestartService);

        private ICommand _killServiceCommand;
        public ICommand KillServiceCommand => _killServiceCommand ??= new RelayCommand(KillServiceExecute, CanExecutedKillService);
        #endregion


        public MainWindowViewModel()
        {
            //Setup ObservableCollection
            _serviceList = new ObservableCollectionEx<ServiceListViewItem>(ServiceListViewItem.GetInstalledServices());
            //BindingOperations.EnableCollectionSynchronization(_serviceList, _serviceListLock);
            //for (int i = 0; i < 1000; i++)
            //{
            //    _serviceList.Add(_serviceList[0]);
            //}


            //Setup service view
            ServiceView = CollectionViewSource.GetDefaultView(_serviceList);
            ServiceView.Filter = o => ServiceViewFilter(o as ServiceListViewItem);

            //Register Messages
            MessengerInstance.Register<UpdateServiceItemMessage>(this, UpdateServiceItemMessageExecute);

            //Start Update Timer
            CreateAndStartGuiTimer();
        }

        private void UpdateServiceItemMessageExecute(UpdateServiceItemMessage obj)
        {
            if (obj.OldServiceItem != null)
            {
                //Remove everything that has the same ID in the list
                var toRemove = _serviceList.Where(x => x.Equals(obj.OldServiceItem)).ToList(); //Create a new list needed!!! (or InvalidOperationException)
                if (toRemove.Count > 0)
                    _serviceList.RemoveRange(toRemove);
            }

            _serviceList.Add(obj.NewServiceItem);
        }


        private void OnLoadedExecute()
        {
            //If Windows 10 1803 installed don't ask for start the UI0Detect service
            if (DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
            {
                //Check interactive service registry key
                try
                {
                    //Ask the user if the key is no set
                    if (RegistryManagement.CheckInteractiveServices())
                        return;


                    MessageBoxResult result = MessageBoxService.Show(ResManager.GetString("interactive_service_regkey_not_set"), ResManager.GetString("question"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return;

                    if (!RegistryManagement.EnableInteractiveServices(true))
                        MessageBoxService.Show(ResManager.GetString("problem_occurred"), ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBoxService.Show(ResManager.GetString("failed_to_set_interServ") + "\n" + ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //Check if UI0Detect service is running
                if (!DaemonMasterUtils.CheckUi0DetectService())
                {
                    MessageBoxService.Show(ResManager.GetString("error_ui0service", CultureInfo.CurrentUICulture),
                         ResManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                         MessageBoxImage.Error);
                }
            }
        }

        private bool ServiceViewFilter(ServiceListViewItem item)
        {
            //Invalid item => filter it out
            if (item == null)
                return false;

            //No filter so show everything
            if (string.IsNullOrWhiteSpace(Search))
                return true;

            //Filter rules
            return item.DisplayName.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private void CanExecutedOpen(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = SelectedService != null; }
        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                using (var serviceController = new ServiceController(SelectedService.ServiceName))
                {
                    MessengerInstance.Send(new OpenEditServiceWindowMessage(this, SelectedService, serviceController.Status != ServiceControllerStatus.Stopped));
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message + "\n StackTrace: " + ex.StackTrace, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                //MessageBoxService.Show(ResManager.GetString("cannot_load_data_from_registry") + "\n" + ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CanExecutedDelete(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = SelectedService != null; }
        private void DeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBoxResult result = MessageBoxService.Show(ResManager.GetString("msg_warning_delete"),
                ResManager.GetString("question"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(SelectedService.ServiceName,
                        Advapi32.ServiceAccessRights.AllAccess))
                    {
                        if (serviceHandle.QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                        {
                            result = MessageBoxService.Show(ResManager.GetString("you_must_stop_the_service_first"),
                                ResManager.GetString("information"), MessageBoxButton.YesNo,
                                MessageBoxImage.Information);

                            if (result != MessageBoxResult.Yes)
                                return;

                            serviceHandle.Stop();
                            try
                            {
                                serviceHandle.WaitForStatus(Advapi32.ServiceCurrentState.Stopped,
                                    TimeSpan.FromSeconds(10));
                            }
                            catch (TimeoutException)
                            {
                                //ignore (DeleteService will throw an exception when something is not ok (with a better error message than mine has ever been xD))
                            }
                        }

                        serviceHandle.DeleteService();
                    }
                }

                _serviceList.Remove(_serviceList.Single(x => x.ServiceName == SelectedService.ServiceName));
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ResManager.GetString("the_service_deletion_was_unsuccessful") + "\n" + ex.Message,
                     ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CanExecutedNewService(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = _serviceList.Count < 200; }
        private void NewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MessengerInstance.Send(new OpenEditServiceWindowMessage(this));
        }

        private bool CanExecutedSwitchToServiceSession() { return DaemonMasterUtils.IsSupportedWindows10VersionForIwd; }
        private void SwitchToServiceSessionExecute()
        {
            if (DaemonMasterUtils.CheckUi0DetectService())
            {
                //if its Windows 10 then showing a warning message
                if (Environment.OSVersion.Version.Major == 10)
                {
                    MessageBoxResult result =
                       MessageBoxService.Show(ResManager.GetString("windows10_mouse_keyboard", CultureInfo.CurrentUICulture),
                            ResManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.OKCancel,
                            MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                        return;
                }

                //Switch to session 0
                Winsta.WinStationSwitchToServicesSession();
            }
            else
            {
                MessageBoxService.Show(
                     ResManager.GetString("failed_start_UI0detect_service", CultureInfo.CurrentUICulture),
                     ResManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                     MessageBoxImage.Error);
            }
        }

        private bool CanExecutedKillService() { return SelectedService != null; }
        private void KillServiceExecute()
        {
            try
            {
                using (ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(SelectedService.ServiceName,
                        Advapi32.ServiceAccessRights.UserDefinedControl | Advapi32.ServiceAccessRights.QueryStatus))
                    {
                        if (serviceHandle.QueryServiceStatus().currentState == Advapi32.ServiceCurrentState.Stopped)
                            return;

                        try
                        {
                            serviceHandle.ExecuteCommand((int)ServiceCommands.ServiceKillProcessAndStop);
                            serviceHandle.WaitForStatus(Advapi32.ServiceCurrentState.Stopped, TimeSpan.FromSeconds(2));
                        }
                        catch (TimeoutException)
                        {
                            if (KillChildProcessJob.IsSupportedWindowsVersion && SelectedService.ServicePid != null)
                            {
                                Process process = Process.GetProcessById((int)SelectedService.ServicePid);
                                process.Kill();
                            }
                            else
                            {
                                MessageBoxService.Show(
                                     ResManager.GetString("cannot_kill_service", CultureInfo.CurrentUICulture),
                                     ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecutedRestartService() { return SelectedService != null; }
        private void RestartServiceExecute()
        {
            try
            {
                using (ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(SelectedService.ServiceName,
                        Advapi32.ServiceAccessRights.QueryStatus | Advapi32.ServiceAccessRights.Start |
                        Advapi32.ServiceAccessRights.Stop))
                    {
                        var state = serviceHandle.QueryServiceStatus().currentState;
                        if (state != Advapi32.ServiceCurrentState.Stopped &&
                            state != Advapi32.ServiceCurrentState.StopPending)
                            serviceHandle.Stop();

                        //Wait for stop
                        serviceHandle.WaitForStatus(Advapi32.ServiceCurrentState.Stopped, TimeSpan.FromSeconds(10));

                        //Start service
                        serviceHandle.Start();
                    }
                }
            }
            catch (TimeoutException)
            {
                MessageBoxService.Show(ResManager.GetString("timeout_exception_service_restart"),
                     ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecutedStopService() { return SelectedService != null; }
        private void StopServiceExecute()
        {
            try
            {
                using (ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle =
                        scm.OpenService(SelectedService.ServiceName, Advapi32.ServiceAccessRights.Stop))
                    {
                        serviceHandle.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteStartService(bool inUserSession)
        {
            return SelectedService != null && (!inUserSession || SelectedService.UseLocalSystem);
        }
        private void StartServiceExecute(bool inUserSession)
        {
            try
            {
                if (inUserSession)
                {
                    //Write username where the service should start the process
                    RegistryManagement.WriteSessionUsername(SelectedService.ServiceName,
                        WindowsIdentity.GetCurrent().Name);
                }

                using (ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle =
                        scm.OpenService(SelectedService.ServiceName, Advapi32.ServiceAccessRights.Start))
                    {
                        serviceHandle.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public override void Cleanup()
        {
            // Unregister its own messages, so that we risk no leak
            //Messenger.Default.Unregister < ...> (this);
            MessengerInstance.Unregister<UpdateServiceItemMessage>(this);

            base.Cleanup();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        GUI Update Timer                                              //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void CreateAndStartGuiTimer()
        {
            var guiDispatcherTimer = new DispatcherTimer();
            guiDispatcherTimer.Interval = TimeSpan.FromSeconds(ConfigManagement.GetConfig.UpdateInterval);
            guiDispatcherTimer.Tick += (sender, args) =>
            {
               _ = UpdateItemsAsync();
            };

            guiDispatcherTimer.Start();

            //Force Update on startup
            _ = UpdateItemsAsync();
        }

        private bool _updateItemsBusy;
        private async Task UpdateItemsAsync()
        {
            if (_updateItemsBusy)
                return;

            _updateItemsBusy = true;
            //stopwatch.Restart();

            IEnumerable<Task> listOfTasks = _serviceList.Select(item => item.UpdateStatusAsync()).ToList();
            await Task.WhenAll(listOfTasks);

            //stopwatch.Stop();
            //Console.WriteLine(stopwatch.ElapsedMilliseconds);
            _updateItemsBusy = false;
        }
    }
}
