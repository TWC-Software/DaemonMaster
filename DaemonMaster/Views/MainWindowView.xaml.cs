using System;
using DaemonMaster.Core;
using DaemonMaster.Utilities.Messages;
using DaemonMaster.ViewModels;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace DaemonMaster.Views
{
    public partial class MainWindowView : IRecipient<OpenEditServiceWindowMessage>
    {
        public MainWindowView(IServiceProvider serviceProvider)
        {
            ViewModelBase? viewModel;
            IServiceScope? serviceScope = serviceProvider.CreateScope();

            //search for type that match
            DataContext = viewModel = ViewModelLocator.Build(serviceScope, this);

            WeakReferenceMessenger.Default.Register(this);

            InitializeComponent();

            // Add Command bindings
            if (viewModel != null)
                CommandBindings.AddRange(viewModel.Commands);

            Closing += (sender, args) =>
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                serviceScope?.Dispose(); //dispose scope on close to prevent memory leak
                serviceScope = null;
            };
        }

        /// <inheritdoc />
        public void Receive(OpenEditServiceWindowMessage message)
        {
            //Check if already one window with the same service is opened (from this instance) => when true show and focus it
            //var editServiceWindows = Application.Current.Windows.OfType<ServiceEditWindow>();
            var editServiceWindows = OwnedWindows.OfType<ServiceEditWindow>();
            var addWindow = editServiceWindows.FirstOrDefault(x => x.WindowIdentifier == (message.ServiceItem?.ServiceName ?? string.Empty));
            if (addWindow != null)
            {
                addWindow.Show();
                addWindow.Focus();
                return;
            }

            //TODO: MVVM also use show later...
            var window = new ServiceEditWindow(message.ServiceItem?.ServiceName == null ? null : RegistryManagement.LoadFromRegistry(message.ServiceItem.ServiceName))
            {
                Owner = this,
                DataContext = new NewEditViewModel(),
                ReadOnlyMode = message.ReadOnlyMode,
                OriginalItem = message.ServiceItem,
                WindowIdentifier = message.ServiceItem?.ServiceName ?? string.Empty,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.Show();
        }
    }
}