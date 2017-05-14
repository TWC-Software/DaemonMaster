/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: JSON MANAGMENT FILE
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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DaemonMasterCore
{
    //Try CATCH REMOVE
    public static class JSONManagment
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             JSON                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region JSON

        public static void ExportList(ObservableCollection<Daemon> daemons)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer);
            saveFileDialog.Filter = "JSON (*.json)|*.json|" +
                                    "All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.AddExtension = true;

            //Wenn eine Datei gewählt worden ist
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveDaemons(daemons, saveFileDialog.FileName);
            }
        }

        public static ObservableCollection<Daemon> ImportList()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer);
            openFileDialog.Filter = "JSON (*.json)|*.json|" +
                                    "All files (*.*)|*.*";

            //Wenn eine Datei gewählt worden ist
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return LoadDaemons(openFileDialog.FileName);
            }

            return new ObservableCollection<Daemon>();
        }

        private static void SaveDaemons(ObservableCollection<Daemon> daemons, string fullPath)
        {
            string json = JsonConvert.SerializeObject(daemons);
            File.WriteAllText(fullPath, json);
            //MessageBox.Show(LanguageSystem.resManager.GetString("cant_save_deamonfile", LanguageSystem.culture) + ex.Message);
        }

        private static ObservableCollection<Daemon> LoadDaemons(string fullPath)
        {
            string json = String.Empty;
            ObservableCollection<Daemon> daemonList = new ObservableCollection<Daemon>();

            if (File.Exists(fullPath))
            {
                json = File.ReadAllText(fullPath);
                daemonList = JsonConvert.DeserializeObject<ObservableCollection<Daemon>>(json);
            }

            return daemonList;
            //.Show(LanguageSystem.resManager.GetString("cant_load_deamonfile", LanguageSystem.culture) + ex.Message);
        }

        #endregion
    }
}
