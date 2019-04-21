/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceEditWindow
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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Language;
using Microsoft.Win32;
using Newtonsoft.Json;
using Tulpep.ActiveDirectoryObjectPicker;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für ServiceEditWindow.xaml
    /// </summary>
    public partial class ServiceEditWindow : Window
    {
        public DmServiceDefinition GetServiceStartInfo() => _tempServiceConfig;

        private const string PLACEHOLDER_PASSWORD = "88301CEB-1E6E-435C-A355-D055F9F8D430";

        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        private ObservableCollection<ServiceInfo> _dependOnServiceObservableCollection;
        private ObservableCollection<ServiceInfo> _allServicesObservableCollection;
        private ObservableCollection<string> _dependOnGroupObservableCollection;
        private ObservableCollection<string> _allGroupsObservableCollection;

        private DmServiceDefinition _tempServiceConfig;
        private bool _createNewService;

        public ServiceEditWindow(DmServiceDefinition daemon)
        {
            InitializeComponent();

            _tempServiceConfig = daemon ?? new DmServiceDefinition(serviceName: null);

            //Create a new service when the service name is empty
            if (string.IsNullOrWhiteSpace(_tempServiceConfig.ServiceName))
                _createNewService = true;

            //Show the information on the UI
            LoadServiceInfos();
        }

        private void LoadServiceInfos()
        {
            #region GeneralTab

            //Set to readonly when it has already a service name
            if (!string.IsNullOrWhiteSpace(_tempServiceConfig.ServiceName))
                TextBoxServiceName.Text = _tempServiceConfig.ServiceName.Substring(13);

            TextBoxServiceName.IsReadOnly = !_createNewService;

            TextBoxDisplayName.Text = _tempServiceConfig.DisplayName;

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
                TextBoxUsername.Text = string.Empty;
                TextBoxPassword.Password = string.Empty;
                CheckBoxUseLocalSystem.IsChecked = true;
            }
            else
            {
                TextBoxUsername.Text = _tempServiceConfig.Credentials.Username;
                TextBoxPassword.Password = _createNewService ? string.Empty : PLACEHOLDER_PASSWORD;
                CheckBoxUseLocalSystem.IsChecked = false;
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
            if (!DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
            {
                CheckBoxInteractDesk.IsChecked = false;
                CheckBoxInteractDesk.IsEnabled = false;
            }
            else
            {
                CheckBoxInteractDesk.IsChecked = _tempServiceConfig.CanInteractWithDesktop;
            }

            #endregion

            #region Dependency Listboxes

            #region DependOnService

            //Load Data into _dependOnServiceObservableCollection
            _dependOnServiceObservableCollection = new ObservableCollection<ServiceInfo>();
            foreach (string dep in _tempServiceConfig.DependOnService)
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
                var serviceInfo = new ServiceInfo
                {
                    DisplayName = service.DisplayName,
                    ServiceName = service.ServiceName
                };

                if (_dependOnServiceObservableCollection.All(x => x.ServiceName != serviceInfo.ServiceName))
                    _allServicesObservableCollection.Add(serviceInfo);
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
                new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    Filter = "Application files (*.exe, *.lnk)|*.exe; *.lnk|" +
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
                    TextBoxUsername.Text = ".\\" + pickerDialog.SelectedObject.Name;  // ".\\" = Local computer
                }
            }
        }

        private void buttonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
            TextBoxParam.Text = Regex.Replace(args, @"\s+", " ");
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private void SaveConfiguration()
        {
            try
            {
                //Path / File not exist
                if (!Directory.Exists(Path.GetDirectoryName(TextBoxFilePath.Text)) ||
                    !File.Exists(TextBoxFilePath.Text))
                {
                    MessageBox.Show(_resManager.GetString("invalid_path", CultureInfo.CurrentUICulture),
                        _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                //Invalid value error
                if (string.IsNullOrWhiteSpace(TextBoxDisplayName.Text) ||
                    string.IsNullOrWhiteSpace(TextBoxServiceName.Text) ||
                    !int.TryParse(TextBoxMaxRestarts.Text, out int maxRestarts) ||
                    !int.TryParse(TextBoxProcessTimeoutTime.Text, out int processKillTime) ||
                    !int.TryParse(TextBoxProcessRestartDelay.Text, out int processRestartDelay) ||
                    !int.TryParse(TextBoxCounterResetTime.Text, out int counterResetTime) ||
                    (CheckBoxIsConsoleApp.IsChecked ?? false) && !(RadioButtonUseCtrlBreak.IsChecked ?? true) &&
                    !(RadioButtonUseCtrlC.IsChecked ?? true))
                {
                    MessageBox.Show(_resManager.GetString("invalid_values", CultureInfo.CurrentUICulture),
                        _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                #region Password/LocalSystem

                if (CheckBoxUseLocalSystem.IsChecked ?? true) // => LocalSystem is null            
                {
                    _tempServiceConfig.Credentials = ServiceCredentials.LocalSystem;
                }
                else if (string.Equals(TextBoxPassword.Password, PLACEHOLDER_PASSWORD) && //Nothing has changed (null safe)
                         string.Equals(TextBoxUsername.Text, _tempServiceConfig.Credentials.Username)) //Nothing has changed (null safe
                {
                    _tempServiceConfig.Credentials = ServiceCredentials.NoChange; //Null stands for nothing has changed 
                }
                else
                {
                    //No date has been written in the textfields
                    if (string.IsNullOrWhiteSpace(TextBoxUsername.Text))
                    {
                        MessageBox.Show(_resManager.GetString("invalid_pw_user", CultureInfo.CurrentUICulture),
                            _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    //When its not a local user...
                    if (!DaemonMasterUtils.IsLocalDomain(TextBoxUsername.Text))
                    {
                        MessageBox.Show(_resManager.GetString("extern_domain_user_error", CultureInfo.CurrentUICulture),
                            _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
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
                _tempServiceConfig.ServiceName = "DaemonMaster_" + TextBoxServiceName.Text;

                _tempServiceConfig.BinaryPath = TextBoxFilePath.Text;

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
                MessageBox.Show(ex.Message, _resManager.GetString("error", CultureInfo.CurrentUICulture),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyConfiguration()
        {
            try
            {
                //Only Check that right if its not the local system
                if (!Equals(_tempServiceConfig.Credentials, ServiceCredentials.LocalSystem))
                {
                    string username = _tempServiceConfig.Credentials.Username;
                    if (string.IsNullOrWhiteSpace(username))
                        username = TextBoxUsername.Text;

                    using (LsaPolicyHandle lsaWrapper = LsaPolicyHandle.OpenPolicyHandle())
                    {
                        bool hasRightToStartAsService = lsaWrapper.EnumeratePrivileges(username).Any(x => x.Buffer == "SeServiceLogonRight");
                        if (!hasRightToStartAsService)
                        {
                            MessageBoxResult result = MessageBox.Show(_resManager.GetString("logon_as_a_service", CultureInfo.CurrentUICulture), _resManager.GetString("question", CultureInfo.CurrentUICulture), MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result != MessageBoxResult.Yes)
                                return;

                            //Give the account the right to start as service
                            lsaWrapper.AddPrivileges(username, "SeServiceLogonRight");
                        }
                    }
                }

                if (_createNewService)
                {
                    using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.CreateService))
                    {
                        scm.CreateService(_tempServiceConfig);

                        //When no exception has been throwed show up a message
                        MessageBox.Show(
                            _resManager.GetString("the_service_installation_was_successful", CultureInfo.CurrentUICulture),
                            _resManager.GetString("success", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Information);
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

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _resManager.GetString("the_service_installation_was_unsuccessful",
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
                    _createNewService = true;
                    using (StreamReader streamReader = File.OpenText(openFileDialog.FileName))
                    {
                        var serializer = new JsonSerializer();
                        _tempServiceConfig = (DmServiceDefinition)serializer.Deserialize(streamReader, typeof(DmServiceDefinition));
                    }

                    LoadServiceInfos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_import_daemon") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                    {
                        var serializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented
                        };
                        serializer.Serialize(streamWriter, _tempServiceConfig);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(_resManager.GetString("cannot_export_daemon") + "\n" + ex.Message, _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}