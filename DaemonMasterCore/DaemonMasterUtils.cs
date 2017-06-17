using System;
using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Media;
using DaemonMasterCore.Win32;

namespace DaemonMasterCore
{
    public static class DaemonMasterUtils
    {
        //Gibt das Icon der Datei zurück
        public static ImageSource GetIcon(string fullPath)
        {
            try
            {
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(fullPath))
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        //From: http://stackoverflow.com/questions/1841790/how-can-a-windows-service-determine-its-servicename, 02.05.2017
        public static String GetServiceName()
        {
            // Calling System.ServiceProcess.ServiceBase::ServiceNamea allways returns
            // an empty string,
            // see https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387024

            // So we have to do some more work to find out our service name, this only works if
            // the process contains a single service, if there are more than one services hosted
            // in the process you will have to do something else

            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            String query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(query);

            foreach (ManagementObject queryObj in searcher.Get())
            {
                return queryObj["Name"].ToString();
            }

            throw new Exception("Can not get the ServiceName");
        }

        //http://www.pinvoke.net/default.aspx/advapi32.QueryServiceStatusEx
        public static NativeMethods.SERVICE_STATUS_PROCESS QueryServiceStatusEx(IntPtr svManager)
        {
            IntPtr buffer = IntPtr.Zero;
            int size = 0;

            try
            {
                NativeMethods.QueryServiceStatusEx(svManager, 0, buffer, size, out size);
                //Reserviere Speicher in der größe von size
                buffer = Marshal.AllocHGlobal(size);

                if (!NativeMethods.QueryServiceStatusEx(svManager, 0, buffer, size, out size))
                    throw new Win32Exception(Marshal.GetLastWin32Error());


                return (NativeMethods.SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(buffer, typeof(NativeMethods.SERVICE_STATUS_PROCESS));
            }
            catch (Exception)
            {
                throw new NotImplementedException();
            }
            finally
            {
                //Gebe Speicher, wenn genutzt, wieder frei
                if (!buffer.Equals(IntPtr.Zero))
                    Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
