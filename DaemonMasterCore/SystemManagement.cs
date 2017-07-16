using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

namespace DaemonMasterCore
{
    public static class SystemManagement
    {
        public static bool ValidateUser(string username, SecureString password)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                return pc.ValidateCredentials(username, SecurityManagement.ConvertSecureStringToString(password));
            }
        }
    }
}
