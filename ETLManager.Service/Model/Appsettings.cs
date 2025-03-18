using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETLManager.Service.Model
{
    public class Appsettings
    {
        public List<WatcherConfig> WatcherConfigs { get; set; }
        public string WatcherFolderPath { get; set; }
        public List<string> DomainName { get; set; }
        public string WatcherFilter { get; set; }
        public string ETLPath { get; set; }
        public string Operatingsystem { get; set; }
        public string PythonExe { get; set; }
        public string PythonScript { get; set; }
    }
}
