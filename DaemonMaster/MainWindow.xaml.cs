/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: MAIN GUI 
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


using DaemonMaster.Language;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Input;
using DaemonMasterCore;



namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Daemon> processCollection = null;

        public MainWindow()
        {
            InitializeComponent();
            //Erstellt die Liste (leere oder mit gespeicherten Elementen)

            //Setzt die standart Sprache
            LanguageSystem.SetCulture("de-DE");

            //Add events
            EditAddWindow.DaemonSavedEvent += EditAddWindow_DaemonSavedEvent;
            EditAddWindow.DaemonEditEvent += EditAddWindow_DaemonEditEvent;



            //Fragt, wenn der RegKey nicht gesetzt ist, ob dieser gesetzt werden soll
            if (!AskToEnableInteractiveServices())
                this.Close();

            //Bei einem Problem bei laden aus der Registry wird eine leere Liste geladen und eine Fehlermeldung angezeigt
            try
            {
                processCollection = RegistryManagment.LoadFromRegistry();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("cant_load_deamonfile", LanguageSystem.culture) + ex.Message);
                processCollection = new ObservableCollection<Daemon>();
            }

            //Add Event
            processCollection.CollectionChanged += ProcessList_CollectionChanged;

            //Aktualisiert die Liste zum start
            listBoxDaemons.ItemsSource = processCollection;

            if (!ServiceManagment.CheckUI0DetectService())
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("error_ui0service", LanguageSystem.culture), LanguageSystem.resManager.GetString("error", LanguageSystem.culture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            if (listBoxDaemons.SelectedItem == null)
                return;

            EditDaemon();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            RemoveDaemon(listBoxDaemons.SelectedItem as Daemon);
        }

        private void buttonFilter_Click(object sender, RoutedEventArgs e)
        {

            foreach (Daemon d in processCollection)
            {
                if (d.DisplayName.Contains(textBoxFilter.Text))
                {
                    listBoxDaemons.SelectedItem = d;
                    break;
                }
            }
        }

        private void buttonSwitchToSession0_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSession0();
        }

        //ListBox
        private void MenuItemStart_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            StartDaemon(listBoxDaemons.SelectedItem as Daemon);
        }

        private void MenuItemStop_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            StopDaemon(listBoxDaemons.SelectedItem as Daemon);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            RemoveDaemon(listBoxDaemons.SelectedItem as Daemon);
        }

        private void listBoxDaemons_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            EditDaemon();
        }

        //MENU

        private void MenuItem_Click_AddDaemon(object sender, RoutedEventArgs e)
        {
            AddDaemon();
        }

        private void MenuItem_Click_RemoveDaemon(object sender, RoutedEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            RemoveDaemon(listBoxDaemons.SelectedItem as Daemon);
        }

        private void MenuItem_Click_EditDaemon(object sender, RoutedEventArgs e)
        {
            if (listBoxDaemons.SelectedItem == null)
                return;

            EditDaemon();
        }

        private void MenuItem_Click_Credits(object sender, RoutedEventArgs e)
        {
            CreditsWindow creditsWindow = new CreditsWindow();
            creditsWindow.ShowDialog();
        }

        private void MenuItem_Click_Export(object sender, RoutedEventArgs e)
        {
            //DaemonMasterCore.ExportList(processCollection);
            MessageBox.Show(LanguageSystem.resManager.GetString("currently_unavailable"), LanguageSystem.resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Click_Import(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(LanguageSystem.resManager.GetString("currently_unavailable"), LanguageSystem.resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Other

        private void ProcessList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) // Wenn sich was ändert kommt es in die Liste
        {
            listBoxDaemons.ItemsSource = processCollection;
        }


        private void OpenAddDaemonWindow()
        {
            EditAddWindow addProcessWindow = new EditAddWindow(); // Neues Event Im EditAddWindow Fenster
            addProcessWindow.ShowDialog(); // Fenster geht auf, Code geht erst weiter wenn Fesnter geschlossen ist
        }

        private void OpenEditDaemonWindow(int index)
        {
            EditAddWindow addProcessWindow = new EditAddWindow(processCollection[index] as Daemon, index);
            addProcessWindow.ShowDialog();
        }


        private bool AskToEnableInteractiveServices()
        {
            //Wenn der RegKey nicht gestetzt ist, soll der Nutzer gefragt werden
            if (!ServiceManagment.CheckNoInteractiveServicesRegKey())
            {
                MessageBoxResult result = MessageBox.Show(LanguageSystem.resManager.GetString("interactive_service_regkey_not_set"), LanguageSystem.resManager.GetString("question"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (ServiceManagment.ActivateInteractiveServices())
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(LanguageSystem.resManager.GetString("problem_occurred"), LanguageSystem.resManager.GetString("error"), MessageBoxButton.OK);
                    }
                }

                return false;
            }

            return true;
        }

        private void AddDaemon()
        {
            if (listBoxDaemons.Items.Count <= 256)
            {
                OpenAddDaemonWindow();
            }
            else
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("max_limit_reached"), LanguageSystem.resManager.GetString("warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveDaemon(Daemon daemon)
        {
            if (daemon == null)
                return;

            try
            {
                ServiceManagment.DeleteService(daemon);
                processCollection.RemoveAt(listBoxDaemons.SelectedIndex);

                MessageBox.Show(LanguageSystem.resManager.GetString("the_service_deletion_was_successful"), LanguageSystem.resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("the_service_deletion_was_unsuccessful"), LanguageSystem.resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDaemon()
        {
            OpenEditDaemonWindow(listBoxDaemons.SelectedIndex);
        }

        private void StartDaemon(Daemon daemon)
        {
            if (daemon == null)
                return;

            switch (ServiceManagment.StartService(daemon))
            {
                case -1:
                    MessageBox.Show(LanguageSystem.resManager.GetString("cannot_start_the_service"), LanguageSystem.resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case 0:
                    MessageBox.Show(LanguageSystem.resManager.GetString("cannot_start_the_service_already_running"), LanguageSystem.resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case 1:
                    MessageBox.Show(LanguageSystem.resManager.GetString("service_start_was_successful"), LanguageSystem.resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void StopDaemon(Daemon daemon)
        {
            if (daemon == null)
                return;

            switch (ServiceManagment.StopService(daemon))
            {
                case -1:
                    MessageBox.Show(LanguageSystem.resManager.GetString("cannot_stop_the_service"), LanguageSystem.resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case 0:
                    MessageBox.Show(LanguageSystem.resManager.GetString("cannot_stop_the_service_already_stopped"), LanguageSystem.resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case 1:
                    MessageBox.Show(LanguageSystem.resManager.GetString("service_stop_was_successful"), LanguageSystem.resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void SwitchToSession0()
        {
            if (ServiceManagment.CheckUI0DetectService())
            {
                MessageBoxResult result = MessageBox.Show(LanguageSystem.resManager.GetString("windows10_mouse_keyboard", LanguageSystem.culture), LanguageSystem.resManager.GetString("warning", LanguageSystem.culture), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    DaemonMasterCore.Win32.WINSTA.WinStationSwitchToServicesSession();
                }
            }
            else
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("failed_start_UI0detect_service", LanguageSystem.culture), LanguageSystem.resManager.GetString("error", LanguageSystem.culture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          EVENT HANDLER                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region EventHandler

        private void EditAddWindow_DaemonSavedEvent(Daemon daemon) // Fügt Deamon Objekt der Liste hinzu
        {
            try
            {
                ServiceManagment.CreateInteractiveService(daemon);
                RegistryManagment.SaveInRegistry(daemon);
                processCollection.Add(daemon);

                MessageBox.Show(LanguageSystem.resManager.GetString("the_service_installation_was_successful", LanguageSystem.culture), LanguageSystem.resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("the_service_installation_was_unsuccessful", LanguageSystem.culture) + "\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAddWindow_DaemonEditEvent(Daemon daemon, int index) // Fügt Deamon Objekt der Liste hinzu
        {
            try
            {
                RegistryManagment.SaveInRegistry(daemon);
                processCollection[index] = daemon;
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("data_cant_be_saved", LanguageSystem.culture) + ex.Message, LanguageSystem.resManager.GetString("error", LanguageSystem.culture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}