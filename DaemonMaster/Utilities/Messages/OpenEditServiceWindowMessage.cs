using DaemonMaster.Models;
using GalaSoft.MvvmLight.Messaging;

namespace DaemonMaster.Utilities.Messages
{
    public class OpenEditServiceWindowMessage : MessageBase
    {
        public ServiceListViewItem ServiceItem { get; }
        public bool ReadOnlyMode { get; }

        public OpenEditServiceWindowMessage(object sender) : this(sender, null, false)
        {
        }

        public OpenEditServiceWindowMessage(object sender, ServiceListViewItem item, bool readOnlyMode) : base(sender)
        {
            ServiceItem = item;
            ReadOnlyMode = readOnlyMode;
        }
    }

}
