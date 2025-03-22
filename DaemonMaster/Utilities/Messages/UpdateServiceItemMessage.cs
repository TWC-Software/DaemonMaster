using DaemonMaster.Models;

namespace DaemonMaster.Utilities.Messages
{
    public class UpdateServiceItemMessage
    {
        public object Sender { get; }
        public ServiceListViewItem? OldServiceItem { get; }
        public ServiceListViewItem NewServiceItem { get; }
        
        public UpdateServiceItemMessage(object sender, ServiceListViewItem? oldItem, ServiceListViewItem newItem)
        {
            Sender = sender;
            OldServiceItem = oldItem;
            NewServiceItem = newItem;
        }
    }
}
