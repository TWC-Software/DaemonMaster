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


using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using DaemonMaster.Language;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für EditAddWindow.xaml
    /// </summary>
    public partial class EditAddWindow : Window
    {
        //Erstellt ein Event 
        internal delegate void DaemonSavedDelegate(Daemon daemon);
        internal delegate void DaemonEditDelegate(Daemon daemon, int index);
        internal static event DaemonSavedDelegate DaemonSavedEvent;
        internal static event DaemonEditDelegate DaemonEditEvent;

        private string fileName = String.Empty;
        private string fileDir = String.Empty;


        private readonly bool onEdit = false;
        private readonly int index = 0;


        public EditAddWindow()
        {
            InitializeComponent();

            textBoxFilePath.IsReadOnly = true;
            checkBoxCustomUsername.IsChecked = false;
            textBoxUsername.IsEnabled = false;
            textBoxPassword.IsEnabled = false;
        }

        public EditAddWindow(Daemon daemon, int index) : this() // This = Konstruktor davor wird auch ausgeführt (=> Ableitung vom Oberen)
        {
            onEdit = true;
            this.index = index;

            textBoxFilePath.Text = daemon.FullPath; // Holt sich Parameter aus dem Deamon und schreibt sie in das Fenster rein

            fileName = daemon.FileName;

            textBoxName.Text = daemon.DisplayName;
            textBoxParam.Text = daemon.Parameter;

            if (daemon.UserName != String.Empty && daemon.UserPassword != String.Empty)
            {
                checkBoxCustomUsername.IsChecked = true;
                textBoxUsername.Text = daemon.UserName;
                textBoxPassword.Text = daemon.UserPassword;
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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer); // @"c:\";
            openFileDialog.Filter = "Exe files (*.exe)|*.exe|" +
                                    "All files (*.*)|*.*";

            //Wenn eine Datei gewählt worden ist
            if (openFileDialog.ShowDialog() == true)
            {
                textBoxFilePath.Text = openFileDialog.FileName;
                fileDir = Path.GetDirectoryName(openFileDialog.FileName);
                fileName = openFileDialog.SafeFileName;

                //Wenn der Name noch leer oder der Standart Name geladen ist, soll er ihn mit dem Datei namen befüllen
                if (textBoxName.Text == String.Empty || textBoxName.Text == "<Please enter a Name> (default = filename)")
                {
                    textBoxName.Text = fileName;
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

                if (Directory.Exists(fileDir) && File.Exists(fileDir + @"\" + fileName))
                {
                    if (textBoxName.Text != String.Empty && textBoxFilePath.Text != String.Empty)
                    {
                        //Erstellt einen neunen "Daemon"
                        Daemon daemon = new Daemon(textBoxName.Text, "DaemonMaster_" + fileName.Split('.')[0], fileDir, fileName);
                        daemon.Parameter = textBoxParam.Text;

                        if (checkBoxCustomUsername.IsChecked ?? false && textBoxUsername.Text != String.Empty && textBoxPassword.Text != String.Empty && textBoxUsername.Text != "<Enter Service Username>" && textBoxPassword.Text != "<Enter Service Password>")
                        {
                            daemon.UserName = textBoxUsername.Text;
                            daemon.UserPassword = textBoxPassword.Text;
                        }
                        else
                        {
                            daemon.UserName = String.Empty;
                            daemon.UserPassword = String.Empty;
                        }

                        if (onEdit)
                        {
                            DaemonEditEvent(daemon, index);
                        }
                        else
                        {
                            DaemonSaved(daemon);
                        }

                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show(LanguageSystem.resManager.GetString("invalid_values", LanguageSystem.culture));
                    }
                }
                else
                {
                    MessageBox.Show(LanguageSystem.resManager.GetString("invalid_path", LanguageSystem.culture));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LanguageSystem.resManager.GetString("error", LanguageSystem.culture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //Event Invoke (Invoke = aufrufen)
        private void DaemonSaved(Daemon daemon)
        {
            DaemonSavedEvent?.Invoke(daemon); // ? = Wenn nicht 0
        }

        #endregion

        private void checkBoxCustomUsername_Checked(object sender, RoutedEventArgs e)
        {
            textBoxUsername.IsEnabled = true;
            textBoxPassword.IsEnabled = true;
        }

        private void checkBoxCustomUsername_Unchecked(object sender, RoutedEventArgs e)
        {
            textBoxUsername.IsEnabled = false;
            textBoxPassword.IsEnabled = false;
        }
    }
}
//[] 