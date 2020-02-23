using System;
using System.Windows;
using TextBox = System.Windows.Controls.TextBox;

namespace DaemonMaster.WPF
{
    public class TextBoxAutoShrink : TextBox
    {
        private double _originalHeight;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _originalHeight = Height;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            Height = double.NaN; //AUTO-Size
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            Height = _originalHeight;
        }
    }
}
