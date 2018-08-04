/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: CreditsWindow
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

using System.Reflection;
using System.Windows;

namespace DaemonMaster
{
    /// <summary>
    /// Interaktionslogik für CreditsWindow.xaml
    /// </summary>
    public partial class CreditsWindow : Window
    {
        public CreditsWindow()
        {
            InitializeComponent();

            textBoxCredits.IsReadOnly = true;
            textBoxCredits.Text =
                "The program \"DeamonMaster\" was originally created by MCPC10 (Main Developer) and Stuffi3000 \n\n GUI: Stuffi3000, MCPC10 \n Code: MCPC10 \n Icon: Stuffi3000 \n\n" +
                "Used Librarys: \n" +
                "=> Newtonsoft.Json - James Newton - King - MIT License \n" +
                "=> NLog - Jaroslaw Kowalski, Kim Christensen, Julian Verdurmen - BSD 3 clause \"New\" or \"Revised\" License \n" +
                "=> AutoUpdater.NET - RBSoft - MIT License \n" +
                "=> Active Directory Object Picker - Tulpep - MS-PL License \n" +
                "=> ListView Layout Manager - Jani Giannoudis - CPOL License \n\n" +
                "Thanks to: \n Pinvoke.net \n stackoverflow.com (for help from the users) \n entwickler-ecke.de (for help from the users)";

            labelVersion.Content = "v" + Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}
