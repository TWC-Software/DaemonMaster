using System;
using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;

namespace DaemonMaster.WPF
{
    public class TextBoxAutoShrink : TextBox
    {
        private double _originalHeight;
        private bool _autoSize;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _originalHeight = Height;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            StartAutoSize();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            if (LineCount > 1)
            {
                StartAutoSize();
            }
            else
            {
                StopAutoSize();
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            StopAutoSize();
        }

        private void StartAutoSize()
        {
            if (_autoSize || LineCount <= 1)
                return;

            _autoSize = true;
            _originalHeight = Height;
            Height = double.NaN; //AUTO-Size
        }

        private void StopAutoSize()
        {
            if (!_autoSize)
                return;

            _autoSize = false;
            Height = _originalHeight;
        }
    }
}
