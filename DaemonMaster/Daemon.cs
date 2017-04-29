/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: DAEMON OBJECT FILE
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


using DaemonMaster.Core;
using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace DaemonMaster
{
    public class Daemon
    {
        private string _name = String.Empty;
        private string _filePath = String.Empty;
        private string _fileName = String.Empty;
        private string _parameter = String.Empty;
        private string _userName = String.Empty;
        private string _userPassword = String.Empty;
        private int _maxRestarts = 3;


        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public string Parameter
        {
            get { return _parameter; }
            set { _parameter = value; }
        }

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        public string UserPassword
        {
            get { return _userPassword; }
            set { _userPassword = value; }
        }

        public int MaxRestarts
        {
            get { return _maxRestarts; }
            set { _maxRestarts = value; }
        }


        [JsonIgnore]
        public ImageSource Icon
        {
            get { return DaemonMasterCore.GetIcon(_filePath); }
        }


        //Konstruktoren
        public Daemon(string name, string filePath, string fileName, string parameter)
        {
            _name = name;
            _filePath = filePath;
            _fileName = fileName;
            _parameter = parameter;
        }

        [JsonConstructor]
        public Daemon()
        {
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
