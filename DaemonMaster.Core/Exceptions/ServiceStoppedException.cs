using System;

namespace DaemonMaster.Core.Exceptions
{
    public class ServiceStoppedException : Exception
    {
        public ServiceStoppedException()
        { }

        public ServiceStoppedException(string message) : base(message)
        { }

        public ServiceStoppedException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
