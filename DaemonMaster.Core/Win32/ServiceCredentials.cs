using System;
using System.Security;

//based on https://github.com/dasMulli/dotnet-win32-service
namespace DaemonMaster.Core.Win32
{
    /// <summary>
    /// A class the represents the logon credentials for a service.
    /// </summary>
    public class ServiceCredentials
    {
        /// <summary>
        /// Give the account name
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Give the password for the account.
        /// </summary>
        public SecureString Password { get; }

        /// <summary>
        /// Create an new <see cref="ServiceCredentials"/> instance.
        /// </summary>
        /// <param name="username">Name of the user</param>
        /// <param name="password">Password of the user as secure string</param>
        public ServiceCredentials(string username, SecureString password)
        {
            Username = username;
            Password = password;
        }

        /// <summary>
        /// With in CreateService LocalSystem will be used with ChangeConfig nothing will be changed with the current user account
        /// </summary>
        public static ServiceCredentials NoChange = new ServiceCredentials(username: null, password: null);

        /// <summary>
        /// Local System is a very high-privileged built-in account. It has extensive privileges on the local system and acts as the computer on the network.
        /// </summary>
        public static ServiceCredentials LocalSystem = new ServiceCredentials(username: @"LocalSystem", password: null);

        /// <summary>
        /// The Local Service account is a built-in account that has the same level of access to resources and objects as members of the Users group. 
        /// This limited access helps safeguard the system if individual services or processes are compromised.
        /// Services that run as the Local Service account access network resources as a null session without credentials.
        /// Be aware that the Local Service account is not supported for the SQL Server or SQL Server Agent services.
        /// </summary>
        public static ServiceCredentials LocalService = new ServiceCredentials(username: @"LocalService", password: null);

        /// <summary>
        /// The Network Service account is a built-in account that has more access to resources and objects than members of the Users group.
        /// Services that run as the Network Service account access network resources by using the credentials of the computer account.
        /// </summary>
        public static ServiceCredentials NetworkService = new ServiceCredentials(username: @"NetworkService", password: null);

        /// <summary>
        /// The Network Service account is a built-in account that has more access to resources and objects than members of the Users group.
        /// Services that run as the Network Service account access network resources by using the credentials of the computer account.
        /// </summary>
        public static ServiceCredentials VirtualAccount = new ServiceCredentials(username: @"NT SERVICE\", password: null);

        /// <summary>
        ///  Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return obj is ServiceCredentials credentials && Equals(credentials);
        }

        /// <summary>
        /// Compare this object with a other service credential in equality.
        /// </summary>
        /// <param name="serviceCredentials"></param>
        /// <returns></returns>
        public bool Equals(ServiceCredentials serviceCredentials)
        {
            return string.Equals(Username, serviceCredentials.Username) && Password.IsEquals(serviceCredentials.Password);
        }

        /// <summary>
        /// Gives the hash of this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Username != null ? Username.GetHashCode() : 0) * 397) ^ (Password != null ? Password.GetHashCode() : 0);
            }
        }
    }
}
