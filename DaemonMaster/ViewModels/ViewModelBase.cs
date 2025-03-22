using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using DaemonMaster.Utilities;

namespace DaemonMaster.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        protected ViewModelBase()
        {
            RelayCommandHelper.RegisterCommandsWithCommandManager(this);
        }

        public virtual CommandBindingCollection Commands => [];
    }
}
