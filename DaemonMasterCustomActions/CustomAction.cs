using DaemonMasterCore;
using Microsoft.Deployment.WindowsInstaller;
using System;

namespace DaemonMasterCustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult UninstallAllServices(Session session)
        {
            session.Log("Begin the unistall of all services");

            try
            {
                session.Log("Killing all services...");
                ServiceManagement.KillAllServices();
                session.Log("Deleting all services...");
                ServiceManagement.DeleteAllServices();
            }
            catch (Exception e)
            {
                session.Log(e.Message);
                return ActionResult.Failure;
            }

            session.Log("Success");
            return ActionResult.Success;
        }
    }
}
