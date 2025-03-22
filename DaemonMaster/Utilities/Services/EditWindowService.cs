using DaemonMaster.Core;
using DaemonMaster.Models;
using DaemonMaster.Views;

namespace DaemonMaster.Utilities.Services
{
    public enum EditWindowServiceCommand
    {
        EditOrCreate,
        ViewOnly
    }

    public interface IEditWindowService
    {
        ServiceListViewItem Show();
        ServiceListViewItem Show(ServiceListViewItem item, EditWindowServiceCommand command);
    }

    internal class EditWindowService : IEditWindowService
    {
        /// <inheritdoc />
        public ServiceListViewItem Show()
        {
            return Show(null);
        }

        /// <inheritdoc />
        public ServiceListViewItem Show(ServiceListViewItem? item, EditWindowServiceCommand command = EditWindowServiceCommand.EditOrCreate)
        {
            //TODO: MVVM
            var dialog = new ServiceEditWindow(item != null ? RegistryManagement.LoadFromRegistry(item.ServiceName) : null)
            {
                OriginalItem = item,
                ReadOnlyMode = command == EditWindowServiceCommand.ViewOnly
            };

            dialog.Show();
            //var result = dialog.ShowDialog();
            //if (result.HasValue && result.Value)
            //{
            //    return new ServiceListViewItem(dialog.GetServiceStartInfo());
            //}

            return null;
        }
    }
}
