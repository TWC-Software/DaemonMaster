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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Controls;

namespace DaemonMasterCore.WPF
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


        public new string Text
        {
            get
            {
                if (base.Text == placeholder)
                    return String.Empty;

                return base.Text;
            }

            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    SetPlaceholder();
                }
                else
                {
                    base.Text = value;
                    Foreground = System.Windows.Media.Brushes.Black;
                }
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
            if (String.IsNullOrWhiteSpace(base.Text))
            {
                base.Text = placeholder;
                Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void RemovePlaceholder()
        {
            if (base.Text == placeholder)
            {
                base.Text = String.Empty;
                Foreground = System.Windows.Media.Brushes.Black;
            }
        }
    }
}
