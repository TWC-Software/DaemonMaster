using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Language;
using DaemonMaster.Models;
using DaemonMaster.Utilities.Messages;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Security;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Messaging;
using Tulpep.ActiveDirectoryObjectPicker;

namespace DaemonMaster.Views
{
    public partial class ServiceEditWindow : Window
    {
        private const string PlaceholderPasswordString = "88301CEB-1E6E-435C-A355-D055F9F8D430";
        private static readonly SecureString PlaceholderPassword = PlaceholderPasswordString.ConvertStringToSecureString();
        private static readonly ResourceManager ResManager = new ResourceManager(typeof(lang));

        private static readonly DependencyProperty MyTitelProperty = DependencyProperty.Register("MyTitel", typeof(string), typeof(ServiceEditWindow), new UIPropertyMetadata(ResManager.GetString("window_edit_add", CultureInfo.CurrentUICulture)));
        private static readonly DependencyProperty ReadOnlyModeProperty = DependencyProperty.Register("ReadOnlyMode", typeof(bool), typeof(ServiceEditWindow), new UIPropertyMetadata(false));
        private static readonly DependencyProperty NewServiceProperty = DependencyProperty.Register("NewService", typeof(bool), typeof(ServiceEditWindow), new UIPropertyMetadata(false));


        public DmServiceDefinition GetServiceStartInfo() => _tempServiceConfig;

        public bool ReadOnlyMode
        {
            get => (bool)GetValue(ReadOnlyModeProperty);
            set
            {
                SetValue(ReadOnlyModeProperty, value);
                MyTitel = ResManager.GetString("window_edit_add", CultureInfo.CurrentUICulture) + (value ? " [" + ResManager.GetString("read_only", CultureInfo.CurrentUICulture) + "]" : string.Empty);
            }
        }


        private string MyTitel
        {
            get => (string)GetValue(MyTitelProperty);
            set => SetValue(MyTitelProperty, value);
        }

        private bool NewService
        {
            get => (bool)GetValue(NewServiceProperty);
            set => SetValue(NewServiceProperty, value);
        }

        private ObservableCollection<ServiceInfo> _dependOnServiceObservableCollection;
        private ObservableCollection<ServiceInfo> _allServicesObservableCollection;
        private ObservableCollection<string> _dependOnGroupObservableCollection;
        private ObservableCollection<string> _allGroupsObservableCollection;

        private DmServiceDefinition _tempServiceConfig;


        public ServiceEditWindow(DmServiceDefinition? daemon)
        {
            InitializeComponent();

            Closing += (sender, args) =>
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                (DataContext as IDisposable)?.Dispose(); // cleanup view model
            };

            _tempServiceConfig = daemon ?? new DmServiceDefinition(serviceName: null);

            //Create a new service when the service name is empty
            if (string.IsNullOrWhiteSpace(_tempServiceConfig.ServiceName))
                NewService = true;

            //Show the information on the UI
            LoadServiceInfos();
        }

        public ServiceListViewItem OriginalItem { get; set; }

        public string WindowIdentifier { get; set; }

        private void SendResult()
        {
            WeakReferenceMessenger.Default.Send(new UpdateServiceItemMessage(this, OriginalItem, new ServiceListViewItem(_tempServiceConfig)));
        }


        private void LoadServiceInfos()
        {
            #region GeneralTab

            //Set to readonly when it has already a service name
            if (!string.IsNullOrWhiteSpace(_tempServiceConfig.ServiceName))
                TextBoxServiceName.Text = _tempServiceConfig.ServiceName;

            //if (!NewService)
            //{
            //    TextBoxServiceName.IsReadOnly = true;
            //    TextBoxServiceName.Foreground = Brushes.Gray;
            //    TextBoxServiceName.BorderBrush = Brushes.LightGray;
            //}

            TextBoxDisplayName.Text = _tempServiceConfig.DisplayName;

            if (!string.IsNullOrWhiteSpace(_tempServiceConfig.WorkingDirectory))
                TextBoxWorkingDir.Text = _tempServiceConfig.WorkingDirectory;

            if (!string.IsNullOrWhiteSpace(_tempServiceConfig.BinaryPath))
                TextBoxFilePath.Text = _tempServiceConfig.BinaryPath;

            TextBoxParam.Text = _tempServiceConfig.Arguments;
            TextBoxDescription.Text = _tempServiceConfig.Description;

            //StartType
            switch (_tempServiceConfig.StartType)
            {
                case Advapi32.ServiceStartType.AutoStart:
                    ComboBoxStartType.SelectedIndex = _tempServiceConfig.DelayedStart ? 1 : 0;
                    break;

                case Advapi32.ServiceStartType.StartOnDemand:
                    ComboBoxStartType.SelectedIndex = 2;
                    break;

                case Advapi32.ServiceStartType.Disabled:
                    ComboBoxStartType.SelectedIndex = 3;
                    break;
                default:
                    ComboBoxStartType.SelectedIndex = 0;
                    break;
            }

            #endregion

            #region CustomUser

            if (Equals(_tempServiceConfig.Credentials, ServiceCredentials.LocalSystem))
            {
                TextBoxUsername.Clear();
                TextBoxPassword.Clear();
                CheckBoxUseLocalSystem.IsChecked = true;
                CheckBoxUseVirtualAccount.IsChecked = false;
            }
            else if (ServiceCredentials.IsVirtualAccount(_tempServiceConfig.Credentials))
            {
                CheckBoxUseVirtualAccount.IsChecked = true;
                CheckBoxUseLocalSystem.IsChecked = false;
                RenewVirtualAccountName();
            }
            else
            {
                TextBoxUsername.Text = _tempServiceConfig.Credentials.Username;
                TextBoxPassword.Password = NewService ? string.Empty : PlaceholderPasswordString;
                CheckBoxUseLocalSystem.IsChecked = false;
                CheckBoxUseVirtualAccount.IsChecked = false;
            }

            #endregion

            #region AdvancedTab


            TextBoxMaxRestarts.Text = _tempServiceConfig.ProcessMaxRestarts.ToString();
            TextBoxProcessTimeoutTime.Text = _tempServiceConfig.ProcessTimeoutTime.ToString();
            TextBoxProcessRestartDelay.Text = _tempServiceConfig.ProcessRestartDelay.ToString();
            TextBoxCounterResetTime.Text = _tempServiceConfig.CounterResetTime.ToString();
            TextBoxLoadOrderGroup.Text = _tempServiceConfig.LoadOrderGroup;

            //Process Priority
            switch (_tempServiceConfig.ProcessPriority)
            {
                case ProcessPriorityClass.Idle:
                    ComboBoxProcessPriority.SelectedIndex = 0;
                    break;
                case ProcessPriorityClass.BelowNormal:
                    ComboBoxProcessPriority.SelectedIndex = 1;
                    break;
                case ProcessPriorityClass.Normal:
                    ComboBoxProcessPriority.SelectedIndex = 2;
                    break;
                case ProcessPriorityClass.AboveNormal:
                    ComboBoxProcessPriority.SelectedIndex = 3;
                    break;
                case ProcessPriorityClass.High:
                    ComboBoxProcessPriority.SelectedIndex = 4;
                    break;
                case ProcessPriorityClass.RealTime:
                    ComboBoxProcessPriority.SelectedIndex = 5;
                    break;
                default:
                    ComboBoxProcessPriority.SelectedIndex = 2;
                    break;
            }

            CheckBoxIsConsoleApp.IsChecked = _tempServiceConfig.IsConsoleApplication;
            RadioButtonUseCtrlC.IsChecked = _tempServiceConfig.UseCtrlC;
            RadioButtonUseCtrlBreak.IsChecked = !_tempServiceConfig.UseCtrlC;


            //Hide check box interact with desktop on not supported systems (windows 10 1803+)
            if (!DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
            {
                CheckBoxInteractDesk.IsChecked = false;
                CheckBoxInteractDesk.IsEnabled = false;
            }
            else
            {
                CheckBoxInteractDesk.IsChecked = _tempServiceConfig.CanInteractWithDesktop;
            }

            CheckBoxUseEventLog.IsChecked = _tempServiceConfig.UseEventLog;

            #endregion

            #region Dependency Listboxes

            #region DependOnService

            //Load Data into _dependOnServiceObservableCollection
            _dependOnServiceObservableCollection = new ObservableCollection<ServiceInfo>();
            foreach (string dep in _tempServiceConfig.DependOnService)
            {
                try
                {
                    var serviceInfo = new ServiceInfo
                    {
                        ServiceName = dep
                    };

                    //Get display name
                    using (var serviceController = new ServiceController(dep))
                    {
                        serviceInfo.DisplayName = serviceController.DisplayName;
                    }

                    _dependOnServiceObservableCollection.Add(serviceInfo);
                }
                catch
                {
                    // ignored -> continue
                }
            }

            //Sort the list in alphabetical order
            ICollectionView collectionView1 = CollectionViewSource.GetDefaultView(_dependOnServiceObservableCollection);
            collectionView1.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            ListBoxDependOnService.ItemsSource = collectionView1;

            #endregion

            #region AllServices

            //Load Data into _allServicesObservableCollection
            _allServicesObservableCollection = new ObservableCollection<ServiceInfo>();
            foreach (ServiceController service in ServiceController.GetServices())
            {
                try
                {
                    var serviceInfo = new ServiceInfo
                    {
                        DisplayName = service.DisplayName,
                        ServiceName = service.ServiceName
                    };

                    if (_dependOnServiceObservableCollection.All(x => x.ServiceName != serviceInfo.ServiceName))
                        _allServicesObservableCollection.Add(serviceInfo);
                }
                catch
                {
                    // ignored -> continue
                }
            }

            //Sort the list in alphabetical order
            ICollectionView collectionView2 = CollectionViewSource.GetDefaultView(_allServicesObservableCollection);
            collectionView2.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            ListBoxAllServices.ItemsSource = collectionView2;

            #endregion

            #region AllGroups

            //Load Data into _allGroupsObservableCollection
            _allGroupsObservableCollection = new ObservableCollection<string>(RegistryManagement.GetAllServiceGroups());
            //Sort the list in alphabetical order
            ICollectionView collectionView3 = CollectionViewSource.GetDefaultView(_allGroupsObservableCollection);
            collectionView3.SortDescriptions.Add(new SortDescription());
            ListBoxAllGroups.ItemsSource = collectionView3;

            #endregion

            #region DependOnGroup

            //Load Data into _dependOnGroupObservableCollection
            _dependOnGroupObservableCollection = new ObservableCollection<string>(_tempServiceConfig.DependOnGroup);
            //Sort the list in alphabetical order
            ICollectionView collectionView4 = CollectionViewSource.GetDefaultView(_dependOnGroupObservableCollection);
            collectionView3.SortDescriptions.Add(new SortDescription());
            ListBoxDependOnGroup.ItemsSource = collectionView4;

            #endregion

            #endregion
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          GUI ELEMENTS                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GUI_Elements

        private void buttonSave_OnClick(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
        }

        private void buttonSearchPath_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog =
                new Microsoft.Win32.OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    Filter = "Application files (*.exe, *.lnk, *.bat)|*.exe; *.lnk; *.bat|" +
                             "All files (*.*)|*.*",
                    AddExtension = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    DereferenceLinks = false,
                    Multiselect = false
                };

            //If a file has been chosen
            if (openFileDialog.ShowDialog() == true)
            {
                TextBoxFilePath.Text = openFileDialog.FileName;

                //If the display name is empty, then it will use the file name
                if (string.IsNullOrWhiteSpace(TextBoxDisplayName.Text))
                {
                    TextBoxDisplayName.Text = Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
                }
            }
        }

        public void buttonSearchWorkingDir_OnClick(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK
                    && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    TextBoxWorkingDir.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonOpenAdop_OnClick(object sender, RoutedEventArgs e)
        {
            using (var pickerDialog = new DirectoryObjectPickerDialog
            {

                AllowedObjectTypes = ObjectTypes.Users,
                DefaultObjectTypes = ObjectTypes.Users,
                AllowedLocations = Locations.LocalComputer,
                DefaultLocations = Locations.LocalComputer,
                MultiSelect = false,
                ShowAdvancedView = true
            })
            {
                if (pickerDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (string.IsNullOrWhiteSpace(pickerDialog.SelectedObject.Upn))
                    {
                        TextBoxUsername.Text = ".\\" + pickerDialog.SelectedObject.Name;  // ".\\" = Local computer
                    }
                    else
                    {
                        //TODO: test...
                        TextBoxUsername.Text = NameTranslator.TranslateUpnToDownLevel(pickerDialog.SelectedObject.Upn);
                    }
                }
            }
        }

        private void buttonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            //DialogResult = false;
            Close();
        }

        private void buttonImport_Click(object sender, RoutedEventArgs e)
        {
            ImportConfiguration();
        }

        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            ExportConfiguration();
        }

        private void CheckBoxUseLocalSystem_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!checkBox.IsLoaded)
                return;

            ClearPasswordAndUsername();
        }

        private void CheckBoxUseVirtualAccount_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (!checkBox.IsLoaded || !checkBox.IsChecked.HasValue)
                return;

            if (checkBox.IsChecked.Value)
            {
                RenewVirtualAccountName();
            }
            else
            {
                ClearPasswordAndUsername();
            }
        }

        private void TextBoxServiceName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            RenewVirtualAccountName();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                      DEPENDENCIES GUI                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region DependenciesGUI

        private void buttonRemoveDependentService_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxDependOnService.SelectedItem == null)
                return;

            _allServicesObservableCollection.Add((ServiceInfo)ListBoxDependOnService.SelectedItem);
            _dependOnServiceObservableCollection.Remove((ServiceInfo)ListBoxDependOnService.SelectedItem);
        }

        private void buttonAddDependentService_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxAllServices.SelectedItem == null)
                return;

            _dependOnServiceObservableCollection.Add((ServiceInfo)ListBoxAllServices.SelectedItem);
            _allServicesObservableCollection.Remove((ServiceInfo)ListBoxAllServices.SelectedItem);
        }

        private void buttonAddDependentGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxAllGroups.SelectedItem == null)
                return;

            _dependOnGroupObservableCollection.Add((string)ListBoxAllGroups.SelectedItem);
            _allGroupsObservableCollection.Remove((string)ListBoxAllGroups.SelectedItem);
        }

        private void buttonRemoveDependentGroup_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxDependOnGroup.SelectedItem == null)
                return;

            _allGroupsObservableCollection.Add((string)ListBoxDependOnGroup.SelectedItem);
            _dependOnGroupObservableCollection.Remove((string)ListBoxDependOnGroup.SelectedItem);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                       AUTOFORMATTING                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Autoformatting

        //Auto formatting the given arguments on focus lost
        private void TextBoxParam_OnLostFocus(object sender, RoutedEventArgs e)
        {
            string args = TextBoxParam.Text;

            if (string.IsNullOrWhiteSpace(args))
                return;

            //Remove leading and trailing white-space characters
            args = args.Trim();

            //Remove double spaces etc
            TextBoxParam.Text = args;// Regex.Replace(args, @"\s+", " ");
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private void RenewVirtualAccountName()
        {
            if (TextBoxUsername == null || (!CheckBoxUseVirtualAccount?.IsChecked ?? false))
                return;

            TextBoxUsername.Text = "NT SERVICE\\" + TextBoxServiceName.Text;
            //TextBoxUsername.Foreground = Brushes.Gray;
            //TextBoxUsername.BorderBrush = Brushes.LightGray;

            TextBoxPassword?.Clear();
        }

        private void ClearPasswordAndUsername()
        {
            TextBoxUsername?.Clear();
            TextBoxPassword?.Clear();
        }

        private void SaveConfiguration()
        {
            try
            {
                //Path / File not exist
                if (!Directory.Exists(Path.GetDirectoryName(TextBoxFilePath.Text)) ||
                    !File.Exists(TextBoxFilePath.Text))
                {
                    MessageBox.Show(ResManager.GetString("invalid_path", CultureInfo.CurrentUICulture),
                        ResManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                //Invalid value error
                if ((!string.IsNullOrWhiteSpace(TextBoxWorkingDir.Text) && !Directory.Exists(TextBoxWorkingDir.Text)) ||
                    string.IsNullOrWhiteSpace(TextBoxDisplayName.Text) ||
                    string.IsNullOrWhiteSpace(TextBoxServiceName.Text) ||
                    !int.TryParse(TextBoxMaxRestarts.Text, out int maxRestarts) ||
                    !int.TryParse(TextBoxProcessTimeoutTime.Text, out int processKillTime) ||
                    !int.TryParse(TextBoxProcessRestartDelay.Text, out int processRestartDelay) ||
                    !int.TryParse(TextBoxCounterResetTime.Text, out int counterResetTime) ||
                    (CheckBoxIsConsoleApp.IsChecked ?? false) && !(RadioButtonUseCtrlBreak.IsChecked ?? true) &&
                    !(RadioButtonUseCtrlC.IsChecked ?? true))
                {
                    MessageBox.Show(ResManager.GetString("invalid_values", CultureInfo.CurrentUICulture),
                        ResManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                #region Password/LocalSystem

                if (CheckBoxUseLocalSystem.IsChecked ?? false) // => LocalSystem is null            
                {
                    _tempServiceConfig.Credentials = ServiceCredentials.LocalSystem;
                }
                else if (CheckBoxUseVirtualAccount.IsChecked ?? false)
                {
                    _tempServiceConfig.Credentials = new ServiceCredentials(TextBoxUsername.Text, null);
                }
                else if (TextBoxPassword.SecurePassword.IsEquals(PlaceholderPassword) && //Nothing has changed (null safe)
                         string.Equals(TextBoxUsername.Text, _tempServiceConfig.Credentials.Username)) //Nothing has changed (null safe)
                {
                    _tempServiceConfig.Credentials = ServiceCredentials.NoChange; //Null stands for nothing has changed 
                }
                else
                {
                    //No date has been written in the textfields
                    if (string.IsNullOrWhiteSpace(TextBoxUsername.Text))
                    {
                        MessageBox.Show(ResManager.GetString("invalid_pw_user", CultureInfo.CurrentUICulture),
                            ResManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    //When its not a local user...
                    if (!DaemonMasterUtils.IsLocalDomain(TextBoxUsername.Text))
                    {
                        MessageBox.Show(ResManager.GetString("extern_domain_user_error", CultureInfo.CurrentUICulture),
                            ResManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    ////Password or username is not correct
                    //if (!DaemonMasterUtils.ValidateUser(TextBoxUsername.Text, TextBoxPassword.SecurePassword))
                    //{
                    //    MessageBox.Show(_resManager.GetString("login_failed", CultureInfo.CurrentUICulture),
                    //        _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                    //        MessageBoxImage.Error);
                    //    return;
                    //}

                    _tempServiceConfig.Credentials = new ServiceCredentials(TextBoxUsername.Text, TextBoxPassword.SecurePassword);
                }

                #endregion

                _tempServiceConfig.DisplayName = TextBoxDisplayName.Text;
                _tempServiceConfig.ServiceName = TextBoxServiceName.Text;

                _tempServiceConfig.BinaryPath = TextBoxFilePath.Text;
                _tempServiceConfig.WorkingDirectory = TextBoxWorkingDir.Text;

                _tempServiceConfig.Arguments = TextBoxParam.Text;
                _tempServiceConfig.Description = TextBoxDescription.Text;
                _tempServiceConfig.LoadOrderGroup = TextBoxLoadOrderGroup.Text;

                _tempServiceConfig.ProcessMaxRestarts = maxRestarts;
                _tempServiceConfig.ProcessTimeoutTime = processKillTime;
                _tempServiceConfig.ProcessRestartDelay = processRestartDelay;
                _tempServiceConfig.CounterResetTime = counterResetTime;

                _tempServiceConfig.DependOnService = _dependOnServiceObservableCollection.Select(x => x.ServiceName).ToArray();
                _tempServiceConfig.DependOnGroup = _dependOnGroupObservableCollection.ToArray();


                switch (ComboBoxProcessPriority.SelectedIndex)
                {
                    case 0:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.Idle;
                        break;
                    case 1:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.BelowNormal;
                        break;
                    case 2:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.Normal;
                        break;
                    case 3:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.AboveNormal;
                        break;
                    case 4:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.High;
                        break;
                    case 5:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.RealTime;
                        break;
                    default:
                        _tempServiceConfig.ProcessPriority = ProcessPriorityClass.Normal;
                        break;
                }

                _tempServiceConfig.IsConsoleApplication = CheckBoxIsConsoleApp.IsChecked ?? false;
                _tempServiceConfig.UseCtrlC = _tempServiceConfig.IsConsoleApplication &&
                                              (RadioButtonUseCtrlC.IsChecked ?? true) &&
                                              !(RadioButtonUseCtrlBreak.IsChecked ?? false);

                _tempServiceConfig.CanInteractWithDesktop = CheckBoxInteractDesk.IsChecked ?? false;
                _tempServiceConfig.UseEventLog = CheckBoxUseEventLog.IsChecked ?? false;

                switch (ComboBoxStartType.SelectedIndex)
                {
                    case 0:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = Advapi32.ServiceStartType.AutoStart;
                        break;
                    case 1:
                        _tempServiceConfig.DelayedStart = true;
                        _tempServiceConfig.StartType = Advapi32.ServiceStartType.AutoStart;
                        break;
                    case 2:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = Advapi32.ServiceStartType.StartOnDemand;
                        break;
                    case 3:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = Advapi32.ServiceStartType.Disabled;
                        break;
                    default:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = Advapi32.ServiceStartType.AutoStart;
                        break;
                }

                //Apply service settings
                ApplyConfiguration();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ResManager.GetString("error", CultureInfo.CurrentUICulture),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyConfiguration()
        {
            try
            {
                //Only set right it is not a build in account
                if (!Equals(_tempServiceConfig.Credentials, ServiceCredentials.LocalSystem) &&
                    !Equals(_tempServiceConfig.Credentials, ServiceCredentials.LocalService) &&
                    !Equals(_tempServiceConfig.Credentials, ServiceCredentials.NetworkService) &&
                    !Equals(_tempServiceConfig.Credentials, ServiceCredentials.NoChange) &&
                    !ServiceCredentials.IsVirtualAccount(_tempServiceConfig.Credentials)) //Normally all NT SERVICE\\... service has that right, so no need to add it.
                {
                    string username = _tempServiceConfig.Credentials.Username;
                    if (string.IsNullOrWhiteSpace(username))
                        username = TextBoxUsername.Text;

                    using (LsaPolicyHandle lsaWrapper = LsaPolicyHandle.OpenPolicyHandle())
                    {
                        bool hasRightToStartAsService = lsaWrapper.EnumeratePrivileges(username).Any(x => x == "SeServiceLogonRight");
                        if (!hasRightToStartAsService)
                        {
                            MessageBoxResult result = MessageBox.Show(ResManager.GetString("logon_as_a_service", CultureInfo.CurrentUICulture), ResManager.GetString("question", CultureInfo.CurrentUICulture), MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result != MessageBoxResult.Yes)
                                return;

                            //Give the account the right to start as service
                            lsaWrapper.AddPrivilege(username, "SeServiceLogonRight");
                        }
                    }
                }

                if (NewService)
                {
                    using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.CreateService))
                    {
                        scm.CreateService(_tempServiceConfig);

                        ////When no exception has been throwed show up a message (no longer)
                        //MessageBox.Show(
                        //    _resManager.GetString("the_service_installation_was_successful", CultureInfo.CurrentUICulture),
                        //    _resManager.GetString("success", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                    {
                        using (ServiceHandle serviceHandle = scm.OpenService(_tempServiceConfig.ServiceName, Advapi32.ServiceAccessRights.AllAccess))
                        {
                            serviceHandle.ChangeConfig(_tempServiceConfig);
                        }
                    }
                }


                //Save settings in registry after no error is occured
                RegistryManagement.SaveInRegistry(_tempServiceConfig);

                //DialogResult = true;

                SendResult();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ResManager.GetString("the_service_installation_was_unsuccessful",
                        CultureInfo.CurrentUICulture) + "\n" + ex.Message, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ImportConfiguration()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Filter = "DaemonMaster Config (*.dmdf)|*.dmdf|" +
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
                    NewService = true;

                    using (StreamReader streamReader = File.OpenText(openFileDialog.FileName))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                    {
                        {
                            var serializer = new JsonSerializer()
                            {
                                TypeNameHandling = TypeNameHandling.None,
                            };

                            _tempServiceConfig = serializer.Deserialize<DmServiceDefinition>(jsonTextReader);
                        }

                        LoadServiceInfos();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ResManager.GetString("cannot_import_daemon") + "\n" + ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportConfiguration()
        {
            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Filter = "DaemonMaster Config (*.dmdf)|*.dmdf|" +
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
                    using (StreamWriter streamWriter = File.CreateText(saveFileDialog.FileName))
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        var serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented,
                            TypeNameHandling = TypeNameHandling.None,
                        };

                        serializer.Serialize(jsonWriter, _tempServiceConfig);
                        jsonWriter.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ResManager.GetString("cannot_export_daemon") + "\n" + ex.Message, ResManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}