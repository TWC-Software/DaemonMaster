using CommunityToolkit.Mvvm.Input;
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
using NaturalSort.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ConfigManagement = DaemonMaster.Config.ConfigManagement;
using TimeoutException = System.TimeoutException;

namespace DaemonMaster.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IRecipient<UpdateServiceItemMessage>
    {
        private readonly IMessageBoxService _messageBoxService;
        private readonly IEditWindowService _editWindowService;
        private readonly ObservableCollectionEx<ServiceListViewItem> _serviceList;
        private bool _updateItemsBusy;

        private CommandBindingCollection? _commands;
        public override CommandBindingCollection Commands
        {
            get
            {
                return _commands ??= new CommandBindingCollection
                {
                    new CommandBinding(ApplicationCommands.New, (sender, args) => New(),  (sender, args) => args.CanExecute = CanNew),
                    new CommandBinding(ApplicationCommands.Delete,(sender, args) => Delete(),  (sender, args) => args.CanExecute = CanDelete),
                    new CommandBinding(ApplicationCommands.Open, (sender, args) => Open(), (sender, args) => args.CanExecute = CanOpen)
                };
            }
        }

        [ObservableProperty]
        private ServiceListViewItem? _selectedService;

        [ObservableProperty]
        private string? _search;

        public ListCollectionView ServiceView { get; }

        public MainWindowViewModel(IMessageBoxService messageBoxService, IEditWindowService editWindowService)
        {
            _messageBoxService = messageBoxService;
            _editWindowService = editWindowService;

            //Setup ObservableCollection
            _serviceList = new ObservableCollectionEx<ServiceListViewItem>(ServiceListViewItem.GetInstalledServices());
            //BindingOperations.EnableCollectionSynchronization(_serviceList, _serviceListLock);
            //for (int i = 0; i < 1000; i++)
            //{
            //    _serviceList.Add(_serviceList[0]);
            //}


            //Setup service view
            ServiceView = (ListCollectionView)CollectionViewSource.GetDefaultView(_serviceList);
            ServiceView.Filter = o => ServiceViewFilter(o as ServiceListViewItem);
            ServiceView.CustomSort = new SortServiceListView();

            //Register Messages
            WeakReferenceMessenger.Default.Register(this);

            //Start Update Timer
            CreateAndStartGuiTimer();
        }

        [RelayCommand]
        private void Loaded()
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


                    MessageBoxResult result = _messageBoxService.Show(lang.interactive_service_regkey_not_set, lang.question, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return;

                    if (!RegistryManagement.EnableInteractiveServices(true))
                        _messageBoxService.Show(lang.problem_occurred, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    _messageBoxService.Show(lang.failed_to_set_interServ + "\n" + ex.Message, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //Check if UI0Detect service is running
                if (!DaemonMasterUtils.CheckUi0DetectService())
                {
                    _messageBoxService.Show(lang.error_ui0service,
                         lang.error, MessageBoxButton.OK,
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


        [RelayCommand(CanExecute = nameof(CanOpen))]
        private void Open()
        {
            try
            {
                using var serviceController = new ServiceController(SelectedService.ServiceName);
                WeakReferenceMessenger.Default.Send(new OpenEditServiceWindowMessage(this, SelectedService, serviceController.Status != ServiceControllerStatus.Stopped));
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message + "\n StackTrace: " + ex.StackTrace, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
                //_messageBoxService.Show(lang.cannot_load_data_from_registry") + "\n" + ex.Message, lang.error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete()
        {
            MessageBoxResult result = _messageBoxService.Show(lang.msg_warning_delete,
                lang.question,
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
                            result = _messageBoxService.Show(lang.you_must_stop_the_service_first,
                                lang.information, MessageBoxButton.YesNo,
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
                _messageBoxService.Show(lang.the_service_deletion_was_unsuccessful + "\n" + ex.Message,
                     lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanNew))]
        private void New()
        {
            WeakReferenceMessenger.Default.Send(new OpenEditServiceWindowMessage(this));
        }

        [RelayCommand(CanExecute = nameof(CanSwitchToServiceSession))]
        private void SwitchToServiceSession()
        {
            if (DaemonMasterUtils.CheckUi0DetectService())
            {
                //if its Windows 10 then showing a warning message
                if (Environment.OSVersion.Version.Major == 10)
                {
                    MessageBoxResult result =
                       _messageBoxService.Show(lang.windows10_mouse_keyboard,
                            lang.warning, MessageBoxButton.OKCancel,
                            MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                        return;
                }

                //Switch to session 0
                Winsta.WinStationSwitchToServicesSession();
            }
            else
            {
                _messageBoxService.Show(
                     lang.failed_start_UI0detect_service,
                     lang.error, MessageBoxButton.OK,
                     MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanKillService))]
        private void KillService()
        {
            try
            {
                using ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect);
                using ServiceHandle serviceHandle = scm.OpenService(SelectedService.ServiceName,
                    Advapi32.ServiceAccessRights.UserDefinedControl | Advapi32.ServiceAccessRights.QueryStatus);
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
                        _messageBoxService.Show(
                            lang.cannot_kill_service,
                            lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanRestartService))]
        private void RestartService()
        {
            try
            {
                using ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect);
                using ServiceHandle serviceHandle = scm.OpenService(SelectedService.ServiceName,
                    Advapi32.ServiceAccessRights.QueryStatus | Advapi32.ServiceAccessRights.Start |
                    Advapi32.ServiceAccessRights.Stop);
                var state = serviceHandle.QueryServiceStatus().currentState;
                if (state != Advapi32.ServiceCurrentState.Stopped &&
                    state != Advapi32.ServiceCurrentState.StopPending)
                    serviceHandle.Stop();

                //Wait for stop
                serviceHandle.WaitForStatus(Advapi32.ServiceCurrentState.Stopped, TimeSpan.FromSeconds(10));

                //Start service
                serviceHandle.Start();
            }
            catch (TimeoutException)
            {
                _messageBoxService.Show(lang.timeout_exception_service_restart,
                     lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStopService))]
        private void StopService()
        {
            try
            {
                using ServiceControlManager scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect);
                using ServiceHandle serviceHandle =
                    scm.OpenService(SelectedService.ServiceName, Advapi32.ServiceAccessRights.Stop);
                serviceHandle.Stop();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartService))]
        private void StartService()
        {
            if (SelectedService == null)
                return;

            try
            {
                using var scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect);
                using ServiceHandle serviceHandle =
                    scm.OpenService(SelectedService.ServiceName, Advapi32.ServiceAccessRights.Start);
                serviceHandle.Start();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartServiceInUserSession))]
        private void StartServiceInUserSession()
        {
            if (SelectedService == null)
                return;

            try
            {
                //Write username where the service should start the process
                RegistryManagement.WriteSessionUsername(SelectedService.ServiceName,
                    WindowsIdentity.GetCurrent().Name);

                using var scm =
                    ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect);
                using ServiceHandle serviceHandle =
                    scm.OpenService(SelectedService.ServiceName, Advapi32.ServiceAccessRights.Start);
                serviceHandle.Start();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, lang.error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshListViewAsync()
        {
            await UpdateItemsAsync();
        }


        private bool CanOpen => SelectedService != null;
        private bool CanDelete => SelectedService != null;
        private bool CanNew => _serviceList.Count < 200;
        private bool CanSwitchToServiceSession => DaemonMasterUtils.IsSupportedWindows10VersionForIwd;
        private bool CanKillService => SelectedService != null;
        private bool CanRestartService => SelectedService != null;
        private bool CanStopService => SelectedService != null;
        private bool CanStartService => SelectedService != null;
        private bool CanStartServiceInUserSession => SelectedService != null && SelectedService.UseLocalSystem;

        partial void OnSearchChanged(string? value)
        {
            ServiceView.Refresh();
        }

        /// <inheritdoc />
        public void Receive(UpdateServiceItemMessage message)
        {
            if (message.OldServiceItem != null)
            {
                //Remove everything that has the same ID in the list
                var toRemove = _serviceList.Where(x => x.Equals(message.OldServiceItem)).ToList(); //Create a new list needed!!! (or InvalidOperationException)
                if (toRemove.Count > 0)
                    _serviceList.RemoveRange(toRemove);
            }

            _serviceList.Add(message.NewServiceItem);
        }

        private void CreateAndStartGuiTimer()
        {
            var guiDispatcherTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(ConfigManagement.GetConfig.UpdateInterval)
            };

            guiDispatcherTimer.Tick += (sender, args) =>
            {
                _ = UpdateItemsAsync();
            };

            guiDispatcherTimer.Start();

            //Force Update on startup
            _ = UpdateItemsAsync();
        }

        private async Task UpdateItemsAsync()
        {
            if (_updateItemsBusy)
                return;

            _updateItemsBusy = true;
            try
            {
                IEnumerable<Task> listOfTasks = _serviceList.Select(item => item.UpdateStatusAsync()).ToList();
                await Task.WhenAll(listOfTasks);
            }
            finally
            {
                _updateItemsBusy = false;
            }
        }
    }

    public class SortServiceListView : IComparer, IComparer<ServiceListViewItem>
    {
        private readonly NaturalSortComparer _naturalSortComparer = new NaturalSortComparer(StringComparison.CurrentCultureIgnoreCase);

        public int Compare(object? o1, object? o2)
        {
            var x = o1 as ServiceListViewItem;
            var y = o2 as ServiceListViewItem;

            if (x == null)
                return 0;

            if (y == null)
                return 0;

            return Compare(x, y);
        }

        public int Compare(ServiceListViewItem? x, ServiceListViewItem? y)
        {
            if (x == null)
                return 0;

            if (y == null)
                return 0;

            int result = 0;
            result = _naturalSortComparer.Compare(x.DisplayName, y.DisplayName);
            if (result != 0)
                return result;

            return _naturalSortComparer.Compare(x.ServiceName, y.ServiceName);
        }
    }
}
