using DaemonMaster.Core;
using DaemonMaster.Utilities.Messages;
using DaemonMaster.ViewModels;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System.Linq;
using System.Windows;

namespace DaemonMaster.Views
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            Messenger.Default.Register<OpenEditServiceWindowMessage>(this, OpenServiceItemMessageExecute);

            InitializeComponent();

            _viewModel = DataContext as MainWindowViewModel;

            // Add Command bindings
            if (_viewModel != null)
            {
                CommandBindings.AddRange(_viewModel.Commands);
            }

            Closing += (sender, args) =>
            {
                (DataContext as ICleanup)?.Cleanup(); // cleanup view model
                Cleanup(); // cleanup view
            };
        }


        private void OpenServiceItemMessageExecute(OpenEditServiceWindowMessage obj)
        {
            //// If it's not the view model of this view => return
            if (!_viewModel.Equals(obj.Sender))
                return;

            //Check if already one window with the same service is opened (from this instance) => when true show and focus it
            //var editServiceWindows = Application.Current.Windows.OfType<ServiceEditWindow>();
            var editServiceWindows = OwnedWindows.OfType<ServiceEditWindow>();
            var addWindow = editServiceWindows.FirstOrDefault(x => x.WindowIdentifier == (obj.ServiceItem?.ServiceName ?? string.Empty));
            if (addWindow != default)
            {
                addWindow.Show();
                addWindow.Focus();
                return;
            }

            DmServiceDefinition data = obj.ServiceItem != null ? RegistryManagement.LoadFromRegistry(obj.ServiceItem.ServiceName) : null;

                //TODO: MVVM also use show later...
            var window = new ServiceEditWindow(data)
            {
                Owner = this,
                DataContext = new NewEditViewModel(),
                ReadOnlyMode = obj.ReadOnlyMode,
                OriginalItem = obj.ServiceItem,
                WindowIdentifier = obj.ServiceItem?.ServiceName ?? string.Empty,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.Show();
        }

        private void Cleanup()
        {
            Messenger.Default.Unregister<OpenEditServiceWindowMessage>(this);
        }
    }
}