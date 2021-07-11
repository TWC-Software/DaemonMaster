using System;
using System.Globalization;
using System.Resources;
using System.ServiceProcess;
using System.Windows.Data;
using DaemonMaster.Language;

namespace DaemonMaster.Utilities.Converters
{
    [ValueConversion(typeof(ServiceControllerStatus), typeof(string))]
    public class ServiceControllerStatusConverter : IValueConverter
    {
        private readonly ResourceManager _resManager = new ResourceManager(typeof(lang));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case ServiceControllerStatus.ContinuePending:
                    return _resManager.GetString("enum_continue_pending");

                case ServiceControllerStatus.Paused:
                    return _resManager.GetString("enum_paused");

                case ServiceControllerStatus.PausePending:
                    return _resManager.GetString("enum_pause_pending");

                case ServiceControllerStatus.Running:
                    return _resManager.GetString("enum_running");

                case ServiceControllerStatus.StartPending:
                    return _resManager.GetString("enum_start_pending");

                case ServiceControllerStatus.Stopped:
                    return _resManager.GetString("enum_stopped");

                case ServiceControllerStatus.StopPending:
                    return _resManager.GetString("enum_stop_pending");

                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
