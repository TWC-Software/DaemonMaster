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
        private ObservableCollection<Daemon> processCollection = null;
        private readonly ResourceManager resManager = new ResourceManager("DaemonMaster.Language.lang", typeof(MainWindow).Assembly);

        public MainWindow()
        {
            //Set the language of the threads
            //Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentCulture;
            //CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
            //CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

            //Initialize GUI
            InitializeComponent();

            //Erstellt die Liste (leere oder mit gespeicherten Elementen)

            //Add events
            EditAddWindow.DaemonSavedEvent += EditAddWindow_DaemonSavedEvent;
            EditAddWindow.DaemonEditEvent += EditAddWindow_DaemonEditEvent;



            //Fragt, wenn der RegKey nicht gesetzt ist, ob dieser gesetzt werden soll
            if (!AskToEnableInteractiveServices())
                this.Close();

            //Bei einem Problem bei laden aus der Registry wird eine leere Liste geladen und eine Fehlermeldung angezeigt
            try
            {
                processCollection = RegistryManagement.LoadFromRegistry();
            }
            catch (Exception ex)
            {
                MessageBox.Show(resManager.GetString("cannot_load_deamonfile", CultureInfo.CurrentUICulture) + ex.Message);
                processCollection = new ObservableCollection<Daemon>();
            }

            //Add Event
            processCollection.CollectionChanged += ProcessList_CollectionChanged;

            //Aktualisiert die Liste zum start
            listBoxDaemons.ItemsSource = processCollection;

            if (!ServiceManagement.CheckUI0DetectService())
            {
                MessageBox.Show(resManager.GetString("error_ui0service", CultureInfo.CurrentUICulture), resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show(resManager.GetString("currently_unavailable"), resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Click_Import(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(resManager.GetString("currently_unavailable"), resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (!ServiceManagement.CheckNoInteractiveServicesRegKey())
            {
                MessageBoxResult result = MessageBox.Show(resManager.GetString("interactive_service_regkey_not_set"), resManager.GetString("question"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (ServiceManagement.ActivateInteractiveServices())
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(resManager.GetString("problem_occurred"), resManager.GetString("error"), MessageBoxButton.OK);
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
                MessageBox.Show(resManager.GetString("max_limit_reached"), resManager.GetString("warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveDaemon(Daemon daemon)
        {
            if (daemon == null)
                return;

            try
            {
                ServiceManagement.DeleteService(daemon);
                processCollection.RemoveAt(listBoxDaemons.SelectedIndex);

                MessageBox.Show(resManager.GetString("the_service_deletion_was_successful"), resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(resManager.GetString("the_service_deletion_was_unsuccessful"), resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

            switch (ServiceManagement.StartService(daemon))
            {
                case -1:
                    MessageBox.Show(resManager.GetString("cannot_start_the_service"), resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case 0:
                    MessageBox.Show(resManager.GetString("cannot_start_the_service_already_running"), resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case 1:
                    MessageBox.Show(resManager.GetString("service_start_was_successful"), resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void StopDaemon(Daemon daemon)
        {
            if (daemon == null)
                return;

            switch (ServiceManagement.StopService(daemon))
            {
                case -1:
                    MessageBox.Show(resManager.GetString("cannot_stop_the_service"), resManager.GetString("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case 0:
                    MessageBox.Show(resManager.GetString("cannot_stop_the_service_already_stopped"), resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case 1:
                    MessageBox.Show(resManager.GetString("service_stop_was_successful"), resManager.GetString("information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void SwitchToSession0()
        {
            if (ServiceManagement.CheckUI0DetectService())
            {
                MessageBoxResult result = MessageBox.Show(resManager.GetString("windows10_mouse_keyboard", CultureInfo.CurrentUICulture), resManager.GetString("warning", CultureInfo.CurrentUICulture), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    DaemonMasterCore.Win32.WINSTA.WinStationSwitchToServicesSession();
                }
            }
            else
            {
                MessageBox.Show(resManager.GetString("failed_start_UI0detect_service", CultureInfo.CurrentUICulture), resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
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
                ServiceManagement.CreateInteractiveService(daemon);
                RegistryManagement.SaveInRegistry(daemon);
                processCollection.Add(daemon);

                MessageBox.Show(resManager.GetString("the_service_installation_was_successful", CultureInfo.CurrentUICulture), resManager.GetString("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(resManager.GetString("the_service_installation_was_unsuccessful", CultureInfo.CurrentUICulture) + "\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAddWindow_DaemonEditEvent(Daemon daemon, int index) // Fügt Deamon Objekt der Liste hinzu
        {
            try
            {
                RegistryManagement.SaveInRegistry(daemon);
                processCollection[index] = daemon;
            }
            catch (Exception ex)
            {
                MessageBox.Show(resManager.GetString("data_cannot_be_saved", CultureInfo.CurrentUICulture) + ex.Message, resManager.GetString("error", CultureInfo.CurrentUICulture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}