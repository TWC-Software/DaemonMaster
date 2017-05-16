using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DaemonMaster
{
    public class PlaceholderTextbox : TextBox
    {
        public PlaceholderTextbox()
        {
            Placeholder = "Placeholder";

            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
        }

        public string Placeholder { get; set; }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            if (String.IsNullOrWhiteSpace(Text))
                Text = Placeholder;
        }

        private void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            if (String.IsNullOrWhiteSpace(Text))
                Text = "";
        }
    }
}
