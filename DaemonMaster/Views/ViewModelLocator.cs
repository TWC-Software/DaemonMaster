using DaemonMaster.ViewModels;
using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DaemonMaster.Views
{
    public static class ViewModelLocator
    {
        public static ViewModelBase? Build(IServiceScope scope, object? view)
        {
            if (view is null)
                return null;

            var name = view.GetType().FullName!.Replace("View", "ViewModel");
            var type = Type.GetType(name);

            if (type == null || !typeof(ViewModelBase).IsAssignableFrom(type) || scope.ServiceProvider.GetRequiredService(type) is not ViewModelBase vm)
                return null;

            return vm;
        }

        public static bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
