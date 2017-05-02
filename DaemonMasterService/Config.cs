using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterService
{
    public class Config
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }

        public string FileDir { get; set; }
        public string FileName { get; set; }
        public string FullPath
        {
            get { return FileDir + @"\" + FileName; }
        }

        public string Parameter { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public int MaxRestarts { get; set; }

        public int ProcessKillTime { get; set; }
        public int ProcessRestartDelay { get; set; }

        public bool ConsoleApplication { get; set; }
        public bool UseCtrlC { get; set; }
    }
}
