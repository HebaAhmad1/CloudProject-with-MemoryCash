using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCloudProject.Models
{
    public class MemoryCacheSettings
    {
        public int Id { get; set; }
        public int Capacity { get; set; }
        public int Hit { get; set; }
        public int Miss { get; set; }
        public long TotalSizeOfItems { get; set; }
        public int TotalItemsNum { get; set; }
        public int NumberOfRequests { get; set; }
    }
}
