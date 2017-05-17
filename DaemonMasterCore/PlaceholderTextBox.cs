using System;
using System.Xaml;
using System.Windows;
using System.Windows.Controls;

namespace DaemonMasterCore
{
    public class PlaceholderTextBox : TextBox
    {
        private string placeholder = "Placeholder";

        public string Placeholder
        {
            get { return placeholder; }
            set
            {
                placeholder = value;
                SetPlaceholder();
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            RemovePlaceholder();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            SetPlaceholder();
        }

        private void SetPlaceholder()
        {
            if (String.IsNullOrWhiteSpace(Text))
            {
                Text = placeholder;
                Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void RemovePlaceholder()
        {
            if (Text == placeholder)
            {
                Text = String.Empty;
                Foreground = System.Windows.Media.Brushes.Black;
            }
        }
    }
}
