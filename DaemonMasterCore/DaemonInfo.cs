using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterCore
{
    public struct DaemonInfo
    {
        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
