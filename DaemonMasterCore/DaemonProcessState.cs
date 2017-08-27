using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterCore
{
    public enum DaemonProcessState
    {
        AlreadyStopped,
        AlreadyStarted,
        Successful,
        Unsuccessful,
    }
}
