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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using DaemonMaster.Language;
using DaemonMasterCore;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
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
        public ServiceStartInfo GetServiceStartInfo() => _tempServiceConfig;

        private const string PLACEHOLDER_PASSWORD = "88301CEB-1E6E-435C-A355-D055F9F8D430";

        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        private ObservableCollection<ServiceInfo> _dependOnServiceObservableCollection;
        private ObservableCollection<ServiceInfo> _allServicesObservableCollection;
        private ObservableCollection<string> _dependOnGroupObservableCollection;
        private ObservableCollection<string> _allGroupsObservableCollection;

        private ServiceStartInfo _tempServiceConfig;
        private bool _createNewService;

        public ServiceEditWindow(ServiceStartInfo daemon)
        {
            InitializeComponent();

            _tempServiceConfig = daemon ?? new ServiceStartInfo();

            //Create a new service when the service name is empty
            if (String.IsNullOrWhiteSpace(_tempServiceConfig.ServiceName))
                _createNewService = true;

            //Show the information on the UI
            LoadServiceInfos();
        }

        private void LoadServiceInfos()
        {
            #region GeneralTab

            //Set to readonly when it has already a servicename
            if (!String.IsNullOrWhiteSpace(_tempServiceConfig.ServiceName))
                textBoxServiceName.Text = _tempServiceConfig.ServiceName.Substring(13);

            textBoxServiceName.IsReadOnly = !_createNewService;

            textBoxDisplayName.Text = _tempServiceConfig.DisplayName;

            if (!String.IsNullOrWhiteSpace(_tempServiceConfig.FileName) &&
                !String.IsNullOrWhiteSpace(_tempServiceConfig.FileDir))
                textBoxFilePath.Text = _tempServiceConfig.FullPath;

            textBoxParam.Text = _tempServiceConfig.Parameter;
            textBoxDescription.Text = _tempServiceConfig.Description;

            //StartType
            switch (_tempServiceConfig.StartType)
            {
                case NativeMethods.SERVICE_START.SERVICE_AUTO_START:
                    comboBoxStartType.SelectedIndex = _tempServiceConfig.DelayedStart ? 1 : 0;
                    break;

                case NativeMethods.SERVICE_START.SERVICE_DEMAND_START:
                    comboBoxStartType.SelectedIndex = 2;
                    break;

                case NativeMethods.SERVICE_START.SERVICE_DISABLED:
                    comboBoxStartType.SelectedIndex = 3;
                    break;
            }

            #endregion

            #region CustomUser

            if (_tempServiceConfig.UseLocalSystem)
            {
                textBoxUsername.Text = String.Empty;
                textBoxPassword.Password = String.Empty;
                checkBoxUseLocalSystem.IsChecked = true;
            }
            else
            {
                textBoxUsername.Text = _tempServiceConfig.Username;
                textBoxPassword.Password = _createNewService ? String.Empty : PLACEHOLDER_PASSWORD;
                checkBoxUseLocalSystem.IsChecked = false;
            }

            #endregion

            #region AdvancedTab


            textBoxMaxRestarts.Text = _tempServiceConfig.MaxRestarts.ToString();
            textBoxProcessTimeoutTime.Text = _tempServiceConfig.ProcessKillTime.ToString();
            textBoxProcessRestartDelay.Text = _tempServiceConfig.ProcessRestartDelay.ToString();
            textBoxCounterResetTime.Text = _tempServiceConfig.CounterResetTime.ToString();

            checkBoxIsConsoleApp.IsChecked = _tempServiceConfig.ConsoleApplication;
            radioButtonUseCtrlC.IsChecked = _tempServiceConfig.UseCtrlC;
            radioButtonUseCtrlBreak.IsChecked = !_tempServiceConfig.UseCtrlC;

            //Hide check box interact with desktop on not supported systems (windows 10 1803+)
            if (!DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
            {
                checkBoxInteractDesk.IsChecked = false;
                checkBoxInteractDesk.IsEnabled = false;
            }
            else
            {
                checkBoxInteractDesk.IsChecked = _tempServiceConfig.CanInteractWithDesktop;
            }

            #endregion

            #region Dependency Listboxes

            #region DependOnService

            //Load Data into _dependOnServiceObservableCollection
            _dependOnServiceObservableCollection = new ObservableCollection<ServiceInfo>();
            foreach (var dep in _tempServiceConfig.DependOnService)
            {
                ServiceInfo serviceInfo = new ServiceInfo
                {
                    DisplayName = DaemonMasterUtils.GetDisplayName(dep),
                    ServiceName = dep
                };

                _dependOnServiceObservableCollection.Add(serviceInfo);
            }

            //Sort the list in alphabetical order
            ICollectionView collectionView1 = CollectionViewSource.GetDefaultView(_dependOnServiceObservableCollection);
            collectionView1.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            listBoxDependOnService.ItemsSource = collectionView1;

            #endregion

            #region AllServices

            //Load Data into _allServicesObservableCollection
            _allServicesObservableCollection = new ObservableCollection<ServiceInfo>();
            foreach (var service in ServiceController.GetServices())
            {
                ServiceInfo serviceInfo = new ServiceInfo
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
            listBoxAllServices.ItemsSource = collectionView2;

            #endregion

            #region AllGroups

            //Load Data into _allGroupsObservableCollection
            _allGroupsObservableCollection = new ObservableCollection<string>(RegistryManagement.GetAllServiceGroups());
            //Sort the list in alphabetical order
            ICollectionView collectionView3 = CollectionViewSource.GetDefaultView(_allGroupsObservableCollection);
            collectionView3.SortDescriptions.Add(new SortDescription());
            listBoxAllGroups.ItemsSource = collectionView3;

            #endregion

            #region DependOnGroup

            //Load Data into _dependOnGroupObservableCollection
            _dependOnGroupObservableCollection = new ObservableCollection<string>(_tempServiceConfig.DependOnGroup);
            //Sort the list in alphabetical order
            ICollectionView collectionView4 = CollectionViewSource.GetDefaultView(_dependOnGroupObservableCollection);
            collectionView3.SortDescriptions.Add(new SortDescription());
            listBoxDependOnGroup.ItemsSource = collectionView4;

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
            OpenFileDialog openFileDialog =
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
                textBoxFilePath.Text = openFileDialog.FileName;

                //If the display name is empty, then it will use the file name
                if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text))
                {
                    textBoxDisplayName.Text = Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
                }
            }
        }

        private void buttonOpenADOP_OnClick(object sender, RoutedEventArgs e)
        {
            using (DirectoryObjectPickerDialog pickerDialog = new DirectoryObjectPickerDialog
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
                    textBoxUsername.Text = pickerDialog.SelectedObject.Name;  // ".\\" = Local computer
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
            if (listBoxDependOnService.SelectedItem == null)
                return;

            _allServicesObservableCollection.Add((ServiceInfo)listBoxDependOnService.SelectedItem);
            _dependOnServiceObservableCollection.Remove((ServiceInfo)listBoxDependOnService.SelectedItem);
        }

        private void buttonAddDependentService_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxAllServices.SelectedItem == null)
                return;

            _dependOnServiceObservableCollection.Add((ServiceInfo)listBoxAllServices.SelectedItem);
            _allServicesObservableCollection.Remove((ServiceInfo)listBoxAllServices.SelectedItem);
        }

        private void buttonAddDependentGroup_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxAllGroups.SelectedItem == null)
                return;

            _dependOnGroupObservableCollection.Add((string)listBoxAllGroups.SelectedItem);
            _allGroupsObservableCollection.Remove((string)listBoxAllGroups.SelectedItem);
        }

        private void buttonRemoveDependentGroup_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxDependOnGroup.SelectedItem == null)
                return;

            _allGroupsObservableCollection.Add((string)listBoxDependOnGroup.SelectedItem);
            _dependOnGroupObservableCollection.Remove((string)listBoxDependOnGroup.SelectedItem);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                       AUTOFORMATTING                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Autoformatting

        //Auto formatting the given arguments on focus lost
        private void TextBoxParam_OnLostFocus(object sender, RoutedEventArgs e)
        {
            string args = textBoxParam.Text;

            if (String.IsNullOrWhiteSpace(args))
                return;

            //Remove leading and trailing white-space characters
            args = args.Trim();

            //Remove double spaces etc
            textBoxParam.Text = Regex.Replace(args, @"\s+", " ");
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
                if (!Directory.Exists(Path.GetDirectoryName(textBoxFilePath.Text)) ||
                    !File.Exists(textBoxFilePath.Text))
                {
                    MessageBox.Show(_resManager.GetString("invalid_path", CultureInfo.CurrentUICulture),
                        _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                //Invalid value error
                if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text) ||
                    String.IsNullOrWhiteSpace(textBoxServiceName.Text) ||
                    !int.TryParse(textBoxMaxRestarts.Text, out var maxRestarts) ||
                    !int.TryParse(textBoxProcessTimeoutTime.Text, out var processKillTime) ||
                    !int.TryParse(textBoxProcessRestartDelay.Text, out var processRestartDelay) ||
                    !int.TryParse(textBoxCounterResetTime.Text, out var counterResetTime) ||
                    (checkBoxIsConsoleApp.IsChecked ?? false) && !(radioButtonUseCtrlBreak.IsChecked ?? true) &&
                    !(radioButtonUseCtrlC.IsChecked ?? true))
                {
                    MessageBox.Show(_resManager.GetString("invalid_values", CultureInfo.CurrentUICulture),
                        _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                #region Password/LocalSystem

                _tempServiceConfig.UseLocalSystem = checkBoxUseLocalSystem.IsChecked ?? true;

                if (_tempServiceConfig.UseLocalSystem ||
                String.Equals(textBoxPassword.Password, PLACEHOLDER_PASSWORD) && //Nothing has changed (null safe)
                String.Equals(textBoxUsername.Text, _tempServiceConfig.Username)) //Nothing has changed (null safe)
                {
                    _tempServiceConfig.Username = null; //null (nothing change in Win32 API)
                    _tempServiceConfig.Password = null; //null (nothing change in Win32 API)
                }
                else
                {
                    //No date has been written in the textfields
                    if (String.IsNullOrWhiteSpace(textBoxUsername.Text) ||
                        String.IsNullOrWhiteSpace(textBoxPassword.Password))
                    {
                        MessageBox.Show(_resManager.GetString("invalid_pw_user", CultureInfo.CurrentUICulture),
                            _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    //When its not a local user...
                    if (!DaemonMasterUtils.IsLocalDomain(textBoxUsername.Text))
                    {
                        MessageBox.Show(_resManager.GetString("extern_domain_user_error", CultureInfo.CurrentUICulture),
                            _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    //Password or username is not correct
                    if (!SystemManagement.ValidateUser(textBoxUsername.Text, textBoxPassword.SecurePassword))
                    {
                        MessageBox.Show(_resManager.GetString("login_failed", CultureInfo.CurrentUICulture),
                            _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    _tempServiceConfig.Username = textBoxUsername.Text;
                    _tempServiceConfig.Password = textBoxPassword.SecurePassword;
                }

                #endregion

                _tempServiceConfig.DisplayName = textBoxDisplayName.Text;
                _tempServiceConfig.ServiceName = "DaemonMaster_" + textBoxServiceName.Text;

                _tempServiceConfig.FileDir = Path.GetDirectoryName(textBoxFilePath.Text);
                _tempServiceConfig.FileName = Path.GetFileName(textBoxFilePath.Text);
                _tempServiceConfig.FileExtension = Path.GetExtension(textBoxFilePath.Text);

                _tempServiceConfig.Parameter = textBoxParam.Text;
                _tempServiceConfig.Description = textBoxDescription.Text;

                _tempServiceConfig.MaxRestarts = maxRestarts;
                _tempServiceConfig.ProcessKillTime = processKillTime;
                _tempServiceConfig.ProcessRestartDelay = processRestartDelay;
                _tempServiceConfig.CounterResetTime = counterResetTime;

                _tempServiceConfig.DependOnService =
                    _dependOnServiceObservableCollection.Select(x => x.ServiceName).ToArray();
                _tempServiceConfig.DependOnGroup = _dependOnGroupObservableCollection.ToArray();

                _tempServiceConfig.ConsoleApplication = checkBoxIsConsoleApp.IsChecked ?? false;
                _tempServiceConfig.UseCtrlC = _tempServiceConfig.ConsoleApplication &&
                                             (radioButtonUseCtrlC.IsChecked ?? true) &&
                                             !(radioButtonUseCtrlBreak.IsChecked ?? false);

                _tempServiceConfig.CanInteractWithDesktop = checkBoxInteractDesk.IsChecked ?? false;

                switch (comboBoxStartType.SelectedIndex)
                {

                    //Automatic
                    case 0:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
                        break;


                    //Automatic with delay
                    case 1:
                        _tempServiceConfig.DelayedStart = true;
                        _tempServiceConfig.StartType = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
                        break;

                    //Manual
                    case 2:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = NativeMethods.SERVICE_START.SERVICE_DEMAND_START;
                        break;

                    //Disabled
                    case 3:
                        _tempServiceConfig.DelayedStart = false;
                        _tempServiceConfig.StartType = NativeMethods.SERVICE_START.SERVICE_DISABLED;
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
                if (!_tempServiceConfig.UseLocalSystem)
                {
                    using (LsaPolicyHandle lsaWrapper = new LsaPolicyHandle())
                    {
                        bool hasRightToStartAsService = lsaWrapper.EnumeratePrivileges(_tempServiceConfig.Username).Any(x => x.Buffer == "SeServiceLogonRight");
                        if (!hasRightToStartAsService)
                        {
                            MessageBoxResult result = MessageBox.Show(_resManager.GetString("logon_as_a_service", CultureInfo.CurrentUICulture), _resManager.GetString("question", CultureInfo.CurrentUICulture), MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result != MessageBoxResult.OK)
                                return;

                            //Give the account the right to start as service
                            lsaWrapper.AddPrivileges(_tempServiceConfig.Username, new[] { "SeServiceLogonRight" });
                        }
                    }
                }

                if (_createNewService)
                {
                    ServiceManagement.CreateInteractiveService(_tempServiceConfig);

                    MessageBox.Show(
                        _resManager.GetString("the_service_installation_was_successful", CultureInfo.CurrentUICulture),
                        _resManager.GetString("success", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ServiceManagement.ChangeServiceConfig(_tempServiceConfig);
                }

                //Save settings in registry after no error is oucoured
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
            OpenFileDialog openFileDialog = new OpenFileDialog
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
                    _tempServiceConfig = SystemManagement.ParseDmdfFile(openFileDialog.FileName);

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
            SaveFileDialog saveFileDialog = new SaveFileDialog
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
                        JsonSerializer serializer = new JsonSerializer
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