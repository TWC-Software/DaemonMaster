using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace DaemonMaster.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public virtual CommandBindingCollection Commands => [];
    }
}
