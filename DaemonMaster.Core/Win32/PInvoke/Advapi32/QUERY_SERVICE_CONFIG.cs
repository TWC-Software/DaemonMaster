using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct QUERY_SERVICE_CONFIG
        {
            public ServiceType serviceType;
            public ServiceStartType startType;
            public ErrorControl errorControl;
            public string binaryPathName;
            public string loadOrderGroup;
            public uint tagId;
            private string dependencies;
            public string serviceStartName;
            public string displayName;

            private string[] DependOnService
            {
                get
                {
                    return dependencies.Split(new[]{'\0'}, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith(ScGroupIdentifier)).ToArray();
                }
            }

            private string[] DependOnGroup
            {
                get
                {
                    return dependencies.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.StartsWith(ScGroupIdentifier)).ToArray();
                }
            }
        }
    }
}
