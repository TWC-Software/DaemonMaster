using DaemonMaster.Models;

namespace DaemonMaster.Utilities.Messages
{
    public class OpenEditServiceWindowMessage
    {
        public object Sender { get; }
        public ServiceListViewItem? ServiceItem { get; }
        public bool ReadOnlyMode { get; }

        public OpenEditServiceWindowMessage(object sender) : this(sender, null, false)
        {
        }

        public OpenEditServiceWindowMessage(object sender, ServiceListViewItem? item, bool readOnlyMode)
        {
            Sender = sender;
            ServiceItem = item;
            ReadOnlyMode = readOnlyMode;
        }
    }

}
