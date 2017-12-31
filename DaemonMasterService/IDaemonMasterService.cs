using System.ServiceModel;

namespace DaemonMasterService
{
    [ServiceContract]
    interface IDaemonMasterService
    {
        [OperationContract]
        int GetServicePID();

        [OperationContract]
        int GetProcessPID();

        [OperationContract]
        void KillProcess();
    }
}
