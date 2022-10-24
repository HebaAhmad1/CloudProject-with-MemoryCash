using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCloudProject.MemoryCacheClasses
{
    public  class MemoryCacheSettingVm
    {
        public  int Hit { get; set; }
        public  int Miss { get; set; }
        public  long TotalSizeOfItems { get; set; }
        public  int TotalItemsNum { get; set; }
    }
}
