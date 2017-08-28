/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: EditAddWindow
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


using DaemonMasterCore;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Windows;
using DaemonMaster.Language;
using DaemonMasterCore.Exceptions;
using Tulpep.ActiveDirectoryObjectPicker;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für EditAddWindow.xaml
    /// </summary>
    public partial class EditAddWindow : Window
    {
        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        //Erstellt ein Event 
        internal delegate void DaemonSavedDelegate(DaemonItem daemon);
        internal static event DaemonSavedDelegate DaemonSavedEvent;
        internal delegate void DaemonEditDelegate(DaemonItem oldDaemonItem, DaemonItem newDaemonItem);
        internal static event DaemonEditDelegate DaemonEditEvent;

        public DaemonItem DaemonItem { get; private set; } = null;
        public DaemonItem OldDaemonItem { get; private set; } = null;
        private readonly bool onEditMode = false;
        private readonly Daemon daemon = null;

        public EditAddWindow()
        {
            InitializeComponent();

            textBoxFilePath.IsReadOnly = true;
            daemon = new Daemon();
        }

        public EditAddWindow(DaemonItem daemonItem) : this() // This = Konstruktor davor wird auch ausgeführt (=> Ableitung vom Oberen)
        {
            textBoxServiceName.IsReadOnly = true;
            OldDaemonItem = daemonItem;

            try
            {
                if (ServiceManagement.StopService(daemonItem.ServiceName) < 0)
                    throw new ServiceNotStoppedException();

                daemon = RegistryManagement.LoadDaemonFromRegistry(daemonItem.ServiceName);
                LoadDataIntoUI(daemon);

                onEditMode = true;
            }
            catch (Exception)
            {
                MessageBox.Show(_resManager.GetString("cannot_load_data_from_registry"), _resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);

                DialogResult = false;
                this.Close();
            }
        }

        public EditAddWindow(Daemon daemon) : this()
        {
            try
            {
                LoadDataIntoUI(daemon);
            }
            catch (Exception)
            {
                DialogResult = false;
                this.Close();
            }
        }


        private void LoadDataIntoUI(Daemon daemon)
        {
            textBoxDisplayName.Text = daemon.DisplayName;
            textBoxServiceName.Text = daemon.ServiceName.Substring(13);
            textBoxFilePath.Text = daemon.FullPath;
            textBoxParam.Text = daemon.Parameter;
            textBoxDescription.Text = daemon.Description;


            if (String.IsNullOrWhiteSpace(daemon.Username) || daemon.UseLocalSystem || daemon.Password == null)
            {
                checkBoxUseLocalSystem.IsChecked = true;
                textBoxPassword.Password = String.Empty;
                textBoxUsername.Text = String.Empty;
            }
            else
            {
                textBoxPassword.Password = "***Super_sicheres_Passwort***";
                textBoxUsername.Text = daemon.Username;
            }


            switch (daemon.StartType)
            {
                case NativeMethods.SERVICE_START.SERVICE_AUTO_START:
                    comboBoxStartType.SelectedIndex = daemon.DelayedStart ? 1 : 0;
                    break;

                case NativeMethods.SERVICE_START.SERVICE_DEMAND_START:
                    comboBoxStartType.SelectedIndex = 2;
                    break;

                case NativeMethods.SERVICE_START.SERVICE_DISABLED:
                    comboBoxStartType.SelectedIndex = 3;
                    break;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          GUI ELEMENTS                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GUI_Elements

        private void buttonSave_OnClick(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void buttonSearchPath_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog =
                new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    Filter = "Exe files (*.exe)|*.exe|" +
                             "All files (*.*)|*.*",
                    AddExtension = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    DereferenceLinks = true,
                    Multiselect = false
                };

            //Wenn eine Datei gewählt worden ist
            if (openFileDialog.ShowDialog() == true)
            {
                textBoxFilePath.Text = openFileDialog.FileName;

                //Wenn der Name noch leer oder der Standart Name geladen ist, soll er ihn mit dem Datei namen befüllen
                if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text))
                {
                    textBoxDisplayName.Text = Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
                }
            }
        }

        private void buttonOpenADOP_OnClick(object sender, RoutedEventArgs e)
        {
            DirectoryObjectPickerDialog pickerDialog = new DirectoryObjectPickerDialog()
            {

                AllowedObjectTypes = ObjectTypes.Users,
                DefaultObjectTypes = ObjectTypes.Users,
                AllowedLocations = Locations.LocalComputer,
                DefaultLocations = Locations.LocalComputer,
                MultiSelect = false,
                ShowAdvancedView = true
            };

            if (pickerDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxUsername.Text = pickerDialog.SelectedObject.Name;
            }
        }

        private void buttonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void buttonLoadShortcut_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog =
                new OpenFileDialog
                {
                    //Show the path of the shortcuts
                    DereferenceLinks = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    AddExtension = true,
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    Filter = "Shortcut (*.lnk)|*.lnk|" +
                             "All files (*.*)|*.*"
                };

            //Wenn eine Datei gewählt worden ist
            if (openFileDialog.ShowDialog() == true)
            {
                if (DaemonMasterUtils.IsShortcut(openFileDialog.FileName))
                {
                    MessageBoxResult result = MessageBox.Show(_resManager.GetString("data_will_be_overwritten", CultureInfo.CurrentUICulture), _resManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        ShortcutInfo shortcutInfo = DaemonMasterUtils.GetShortcutInfos(openFileDialog.FileName);
                        textBoxParam.Text = shortcutInfo.Arguments;
                        textBoxFilePath.Text = shortcutInfo.FilePath;

                        if (String.IsNullOrWhiteSpace(textBoxDescription.Text))
                        {
                            textBoxDescription.Text = shortcutInfo.Description;
                        }

                        if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text))
                        {
                            textBoxDisplayName.Text = Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(_resManager.GetString("invalid_shortcut", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private void Save()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(textBoxFilePath.Text)) || !File.Exists(textBoxFilePath.Text))
                {
                    MessageBox.Show(_resManager.GetString("invalid_path", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text) ||
                    String.IsNullOrWhiteSpace(textBoxServiceName.Text))
                {
                    MessageBox.Show(_resManager.GetString("invalid_values", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!(bool)checkBoxUseLocalSystem.IsChecked)
                {
                    if (String.IsNullOrWhiteSpace(textBoxUsername.Text) ||
                        String.IsNullOrWhiteSpace(textBoxPassword.Password))
                    {
                        MessageBox.Show(_resManager.GetString("invalid_values", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (textBoxPassword.Password != "***Super_sicheres_Password***")
                    {
                        if (!SystemManagement.ValidateUserWin32(textBoxUsername.Text,
                            SecurityManagement.ConvertStringToSecureString(textBoxPassword.Password)))
                        {
                            MessageBox.Show(_resManager.GetString("invalid_user", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        daemon.Username = textBoxUsername.Text;
                        daemon.Password = textBoxPassword.SecurePassword;
                    }
                    else
                    {
                        if (!SystemManagement.ValidateUserWin32(textBoxUsername.Text, daemon.Password))
                        {
                            MessageBox.Show(_resManager.GetString("invalid_user", CultureInfo.CurrentUICulture), _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
                else
                {
                    daemon.Username = String.Empty;
                    daemon.Password = null;
                }

                string fileDir = Path.GetDirectoryName(textBoxFilePath.Text);
                string fileName = Path.GetFileName(textBoxFilePath.Text);
                string fileExtension = Path.GetExtension(textBoxFilePath.Text);

                daemon.UseLocalSystem = (bool)checkBoxUseLocalSystem.IsChecked;

                daemon.DisplayName = textBoxDisplayName.Text;
                daemon.ServiceName = "DaemonMaster_" + textBoxServiceName.Text;

                daemon.FileDir = fileDir;
                daemon.FileName = fileName;
                daemon.FileExtension = fileExtension;

                daemon.Parameter = textBoxParam.Text;
                daemon.Description = textBoxDescription.Text;

                switch (comboBoxStartType.SelectedIndex)
                {

                    //Automatic
                    case 0:
                        daemon.DelayedStart = false;
                        daemon.StartType = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
                        break;


                    //Automatic with delay
                    case 1:
                        daemon.DelayedStart = true;
                        daemon.StartType = NativeMethods.SERVICE_START.SERVICE_AUTO_START;
                        break;

                    //Manual
                    case 2:
                        daemon.DelayedStart = false;
                        daemon.StartType = NativeMethods.SERVICE_START.SERVICE_DEMAND_START;
                        break;

                    //Disabled
                    case 3:
                        daemon.DelayedStart = false;
                        daemon.StartType = NativeMethods.SERVICE_START.SERVICE_DISABLED;
                        break;
                }




                if (!onEditMode)
                {
                    try
                    {
                        ServiceManagement.CreateInteractiveService(daemon);
                        RegistryManagement.SaveInRegistry(daemon);

                        DaemonItem = new DaemonItem
                        {
                            DisplayName = daemon.DisplayName,
                            ServiceName = daemon.ServiceName,
                            FullPath = daemon.FullPath
                        };

                        MessageBox.Show(
                            _resManager.GetString("the_service_installation_was_successful",
                                CultureInfo.CurrentUICulture), _resManager.GetString("success"), MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        DialogResult = true;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            _resManager.GetString("the_service_installation_was_unsuccessful",
                                CultureInfo.CurrentUICulture) + "\n" + ex.Message, "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                else
                {
                    try
                    {
                        ServiceManagement.ChangeServiceConfig(daemon);
                        RegistryManagement.SaveInRegistry(daemon);

                        DaemonItem = new DaemonItem
                        {
                            DisplayName = daemon.DisplayName,
                            ServiceName = daemon.ServiceName,
                            FullPath = daemon.FullPath
                        };

                        DialogResult = true;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            _resManager.GetString("data_cannot_be_saved", CultureInfo.CurrentUICulture) + ex.Message,
                            _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
//[] 