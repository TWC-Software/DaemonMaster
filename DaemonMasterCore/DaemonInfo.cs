using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DaemonMasterCore
{
    public struct DaemonInfo
    {
        public override string ToString()
        {
            return DisplayName;
        }

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }
        public string FullPath { get; set; }

        public ImageSource Icon => DaemonMasterUtils.GetIcon(FullPath);
    }
}
