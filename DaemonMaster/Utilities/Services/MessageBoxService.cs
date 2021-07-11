using System.Windows;

namespace DaemonMaster.Utilities.Services
{
    public interface IMessageBoxService
    {
        MessageBoxResult Show(string messageBoxText);
        MessageBoxResult Show(string messageBoxText, string caption);
        MessageBoxResult Show(Window owner, string messageBoxText);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button);
        MessageBoxResult Show(Window owner, string messageBoxText, string caption);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
        MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult);
        MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options);
        MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult);
        MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options);
    }

    public class MessageBoxService : IMessageBoxService
    {
        public MessageBoxResult Show(string messageBoxText)
        {
            return MessageBox.Show(messageBoxText);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return MessageBox.Show(owner, messageBoxText);
        }

        public MessageBoxResult Show(string messageBoxText, string caption)
        {
            return MessageBox.Show(messageBoxText, caption);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return MessageBox.Show(owner, messageBoxText, caption);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return MessageBox.Show(messageBoxText, caption, button);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button)
        {
            return MessageBox.Show(owner, messageBoxText, caption, button);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(owner, messageBoxText, caption, button, icon);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon, defaultResult);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon, defaultResult, options);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult, options);
        }
    }
}
