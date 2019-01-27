using System;

namespace DaemonMaster.Core.Exceptions
{
    public class ServiceNotStoppedException : Exception
    {
        public ServiceNotStoppedException()
        { }

        public ServiceNotStoppedException(string message) : base(message)
        { }

        public ServiceNotStoppedException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
