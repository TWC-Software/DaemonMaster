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
