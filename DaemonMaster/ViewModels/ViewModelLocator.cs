/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:DaemonMaster"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using CommonServiceLocator;
using DaemonMaster.Utilities.Services;
using GalaSoft.MvvmLight.Ioc;

namespace DaemonMaster.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    internal class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            //if (ViewModelBase.IsInDesignModeStatic)
            //{
            //    //Create design time view services and models
            //    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            //}
            //else
            //{
            //    // Create run time view services and models
            //    SimpleIoc.Default.Register<IDataService, DataService>();
            //}

            SimpleIoc.Default.Register<IMessageBoxService, MessageBoxService>();
            SimpleIoc.Default.Register<IEditWindowService, EditWindowService>();

            SimpleIoc.Default.Register<MainWindowViewModel>();
            SimpleIoc.Default.Register<NewEditViewModel>();
        }

        public MainWindowViewModel Main => ServiceLocator.Current.GetInstance<MainWindowViewModel>();
        public NewEditViewModel ServiceEdit => ServiceLocator.Current.GetInstance<NewEditViewModel>();

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}