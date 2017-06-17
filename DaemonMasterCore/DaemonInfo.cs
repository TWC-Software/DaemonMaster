using System.Windows.Media;

namespace DaemonMasterCore
{
    public class DaemonInfo
    {
        private ImageSource _icon = null;
        private string _fullPath = null;

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                //Get the new Icon
                _icon = DaemonMasterUtils.GetIcon(value);
            }
        }

        public ImageSource Icon => _icon;

    }
}
