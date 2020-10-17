/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: PlaceholderTextBox
//  
//  This file is part of DeamonMaster.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

namespace DaemonMaster.WPF
{
    public class WatermarkTextBoxAutoShrink : WatermarkTextBox
    {
        private double _originalHeight;
        private bool _autoSize;

        public WatermarkTextBoxAutoShrink()
        {
            Loaded += (sender, args) => _originalHeight = Height;
        }

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
