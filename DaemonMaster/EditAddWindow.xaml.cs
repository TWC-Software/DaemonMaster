/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: EDIT/ADD GUI 
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


using System;
using System.CodeDom;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows;
using DaemonMasterCore;
using DaemonMasterCore.Win32;
using Microsoft.Win32;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für EditAddWindow.xaml
    /// </summary>
    public partial class EditAddWindow : Window
    {
        private readonly ResourceManager resManager = new ResourceManager("DaemonMaster.Language.lang", typeof(EditAddWindow).Assembly);

        //Erstellt ein Event 
        internal delegate void DaemonSavedDelegate(DaemonInfo daemon);
        internal static event DaemonSavedDelegate DaemonSavedEvent;

        private Daemon daemon = null;

        private readonly bool onEdit = false;


        public EditAddWindow()
        {
            InitializeComponent();

            textBoxFilePath.IsReadOnly = true;
            daemon = new Daemon();
        }

        public EditAddWindow(DaemonInfo daemonInfo) : this() // This = Konstruktor davor wird auch ausgeführt (=> Ableitung vom Oberen)
        {
            textBoxServiceName.IsReadOnly = true;


            try
            {
                if (ServiceManagement.StopService(daemonInfo.ServiceName) < 0)
                    throw new Exception("Service must be stopped");

                daemon = RegistryManagement.LoadDaemonFromRegistry(daemonInfo.ServiceName);
                LoadDataIntoUI(daemon);

                onEdit = true;
            }
            catch (Exception)
            {
                MessageBox.Show(resManager.GetString("cannot_load_data_from_registry"), resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);

                this.Close();
            }
        }

        private void LoadDataIntoUI(Daemon daemon)
        {
            textBoxDisplayName.Text = daemon.DisplayName;
            textBoxServiceName.Text = daemon.ServiceName.Substring(13);
            textBoxFilePath.Text = daemon.FullPath;
            textBoxParam.Text = daemon.Parameter;
            textBoxDescription.Text = "";
            textBoxPassword.Text = daemon.UserPassword;
            textBoxUsername.Text = daemon.UserName;
            textBoxDescription.Text = daemon.Description;

            switch (daemon.StartType)
            {
                case ADVAPI.SERVICE_START.SERVICE_AUTO_START:
                    comboBoxStarttyp.SelectedIndex = daemon.DelayedStart ? 1 : 0;
                    break;

                case ADVAPI.SERVICE_START.SERVICE_DEMAND_START:
                    comboBoxStarttyp.SelectedIndex = 2;
                    break;

                case ADVAPI.SERVICE_START.SERVICE_DISABLED:
                    comboBoxStarttyp.SelectedIndex = 3;
                    break;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          GUI ELEMENTS                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GUI_Elements

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void buttonSearchPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog =
                new OpenFileDialog
                {
                    //Show the path of the shortcuts
                    DereferenceLinks = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    AddExtension = true,
                    InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer),
                    Filter = "Exe files (*.exe)|*.exe|" + "Shortcut (*.lnk)|*.lnk|" +
                             "All files (*.*)|*.*"
                };

            //Wenn eine Datei gewählt worden ist
            if (openFileDialog.ShowDialog() == true)
            {
                textBoxFilePath.Text = openFileDialog.FileName;

                //Wenn der Name noch leer oder der Standart Name geladen ist, soll er ihn mit dem Datei namen befüllen
                if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text))
                {
                    textBoxDisplayName.Text = openFileDialog.SafeFileName;
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
                if (!Directory.Exists(Path.GetDirectoryName(textBoxFilePath.Text)) || !System.IO.File.Exists(textBoxFilePath.Text))
                {
                    MessageBox.Show(resManager.GetString("invalid_path", CultureInfo.CurrentUICulture));
                    return;
                }

                if (String.IsNullOrWhiteSpace(textBoxDisplayName.Text) ||
                    String.IsNullOrWhiteSpace(textBoxServiceName.Text))
                {
                    MessageBox.Show(resManager.GetString("invalid_values", CultureInfo.CurrentUICulture));
                    return;
                }

                string fileDir = Path.GetDirectoryName(textBoxFilePath.Text);
                string fileName = Path.GetFileName(textBoxFilePath.Text);
                string fileExtension = Path.GetExtension(textBoxFilePath.Text);


                daemon.DisplayName = textBoxDisplayName.Text;
                daemon.ServiceName = "DaemonMaster_" + textBoxServiceName.Text;

                daemon.FileDir = fileDir;
                daemon.FileName = fileName;
                daemon.FileExtension = fileExtension;

                daemon.Parameter = textBoxParam.Text;
                daemon.Description = textBoxDescription.Text;

                switch (comboBoxStarttyp.SelectedIndex)
                {

                    //Automatic
                    case 0:
                        daemon.DelayedStart = false;
                        daemon.StartType = ADVAPI.SERVICE_START.SERVICE_AUTO_START;
                        break;


                    //Automatic with delay
                    case 1:
                        daemon.DelayedStart = true;
                        daemon.StartType = ADVAPI.SERVICE_START.SERVICE_AUTO_START;
                        break;

                    //Manual
                    case 2:
                        daemon.DelayedStart = false;
                        daemon.StartType = ADVAPI.SERVICE_START.SERVICE_DEMAND_START;
                        break;

                    //Disabled
                    case 3:
                        daemon.DelayedStart = false;
                        daemon.StartType = ADVAPI.SERVICE_START.SERVICE_DISABLED;
                        break;
                }




                if (!onEdit)
                {
                    try
                    {
                        ServiceManagement.CreateInteractiveService(daemon);
                        SaveAllData(daemon);

                        DaemonInfo daemonInfo = new DaemonInfo
                        {
                            DisplayName = daemon.DisplayName,
                            ServiceName = daemon.ServiceName,
                            FullPath = daemon.FullPath
                        };

                        OnDaemonSavedEvent(daemonInfo);

                        MessageBox.Show(
                            resManager.GetString("the_service_installation_was_successful",
                                CultureInfo.CurrentUICulture), resManager.GetString("success"), MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            resManager.GetString("the_service_installation_was_unsuccessful",
                                CultureInfo.CurrentUICulture) + ex.Message, "Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                else
                {
                    try
                    {
                        SaveAllData(daemon);
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            resManager.GetString("data_cannot_be_saved", CultureInfo.CurrentUICulture) + ex.Message,
                            resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        private static void SaveAllData(Daemon daemon)
        {
            RegistryManagement.SaveInRegistry(daemon);
            ServiceManagement.ChangeCompleteServiceConfig(daemon);
        }













        private void checkBoxUseLocalSystem_Checked(object sender, RoutedEventArgs e)
        {
            textBoxUsername.IsEnabled = false;
            textBoxPassword.IsEnabled = false;
        }

        private void checkBoxUseLocalSystem_Unchecked(object sender, RoutedEventArgs e)
        {
            textBoxUsername.IsEnabled = true;
            textBoxPassword.IsEnabled = true;
        }

        private static void OnDaemonSavedEvent(DaemonInfo daemonInfo)
        {
            DaemonSavedEvent?.Invoke(daemonInfo);
        }
    }
}
//[] 