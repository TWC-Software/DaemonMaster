/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: LANGUAGE FILE 
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMaster.Language
{
    internal static class LanguageSystem
    {
        internal static ResourceManager resManager = new ResourceManager("DaemonMaster.Language.lang", typeof(MainWindow).Assembly);
        internal static CultureInfo culture = null;

        internal static void SetCulture(string lang)
        {
            try
            {
                if (lang == String.Empty)
                {
                    culture = CultureInfo.CurrentCulture;
                }
                else
                {
                    culture = CultureInfo.CreateSpecificCulture(lang.ToString());
                }

                DaemonMaster.Properties.Resources.Culture = culture;
            }
            catch (Exception)
            {
                throw new NotImplementedException();
            }
        }
    }
}
