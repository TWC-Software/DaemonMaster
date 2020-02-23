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
using System.Windows.Media;

namespace DaemonMaster.WPF
{
    public class PlaceholderTextBoxAutoShrink : TextBox
    {
        private double _originalHeight;
        private string _placeholder = "Placeholder";

        public string Placeholder
        {
            get { return _placeholder; }
            set
            {
                _placeholder = value;
                SetPlaceholder();
            }
        }


        public new string Text
        {
            get
            {
                if (base.Text == _placeholder)
                    return string.Empty;

                return base.Text;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    SetPlaceholder();
                }
                else
                {
                    base.Text = value;
                    Foreground = Brushes.Black;
                }
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _originalHeight = Height;
        }


        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            RemovePlaceholder();
            Height = double.NaN; //AUTO-Size
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            SetPlaceholder();
            Height = _originalHeight;
        }

        private void SetPlaceholder()
        {
            if (string.IsNullOrWhiteSpace(base.Text))
            {
                base.Text = _placeholder;
                Foreground = Brushes.Gray;
            }
        }

        private void RemovePlaceholder()
        {
            if (base.Text == _placeholder)
            {
                base.Text = string.Empty;
                Foreground = Brushes.Black;
            }
        }
    }
}
