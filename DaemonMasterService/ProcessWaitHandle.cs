using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterService
{
    internal class ProcessWaitHandle : WaitHandle
    {
        internal ProcessWaitHandle(SafeProcessHandle handle)
        {
            SafeWaitHandle = new SafeWaitHandle(handle.DangerousGetHandle(), false);
        }
    }
}
