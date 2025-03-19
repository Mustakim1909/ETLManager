using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETLManager.Service.Model
{
    public class FileNamePrefix
    {
        public string SourceName { get; set; }
        public string FinPutSource { get; set; }
        public string FinPutProcessed { get; set; }
    }
}
