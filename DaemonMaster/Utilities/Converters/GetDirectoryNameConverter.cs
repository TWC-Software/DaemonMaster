/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: InvertBooleanConverter
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
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace DaemonMaster.Utilities.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class GetDirectoryNameConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string filePath))
                throw new InvalidOperationException("The target must be a boolean!");

            if(string.IsNullOrEmpty(filePath))
                return null;

            return Path.GetDirectoryName(filePath);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
