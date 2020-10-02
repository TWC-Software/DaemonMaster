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

using System.Windows;
using Xceed.Wpf.Toolkit;

namespace DaemonMaster.WPF
{
    public class WatermarkTextBoxAutoShrink : WatermarkTextBox
    {
        private double _originalHeight;

        public WatermarkTextBoxAutoShrink()
        {
            Loaded += (sender, args) => _originalHeight = Height;
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
