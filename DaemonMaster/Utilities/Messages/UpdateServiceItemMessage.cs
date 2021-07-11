using DaemonMaster.Models;
using GalaSoft.MvvmLight.Messaging;

namespace DaemonMaster.Utilities.Messages
{
    public class UpdateServiceItemMessage : MessageBase
    {
        public ServiceListViewItem OldServiceItem { get; }
        public ServiceListViewItem NewServiceItem { get; }
        
        public UpdateServiceItemMessage(object sender, ServiceListViewItem oldItem, ServiceListViewItem newItem) : base(sender)
        {
            OldServiceItem = oldItem;
            NewServiceItem = newItem;
        }
    }
}
