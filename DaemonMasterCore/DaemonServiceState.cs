using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterCore
{
    public enum DaemonServiceState
    {
        AlreadyStopped,
        AlreadyStarted,
        Successful,
        Unsuccessful,
    }
}
