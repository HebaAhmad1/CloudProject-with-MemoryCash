using FirstCloudProject.MemoryCacheClasses;
using FirstCloudProject.Models;
using FirstCloudProject.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FirstCloudProject.Services
{
    public class HangFireServices
    {
        private readonly MyMemoryCache _memoryCache;
        private readonly MemoryCacheSettingVm _memoryCacheSettingVm;
        private readonly ApplicationDbContext _context;

        public HangFireServices(MyMemoryCache myMemoryCache, MemoryCacheSettingVm memoryCacheSettingVm, ApplicationDbContext applicationDbContext)
        {
            _memoryCache = myMemoryCache;
            _memoryCacheSettingVm = memoryCacheSettingVm;
            _context = applicationDbContext;
        }
        public async Task CreateBackground()
        {

            var mainCace = _context.MemoryCacheSettings.ToList().FirstOrDefault();
            if (mainCace != null)
            {
                mainCace.Hit = _memoryCacheSettingVm.Hit;
                mainCace.Miss = _memoryCacheSettingVm.Miss;
                mainCace.TotalItemsNum = _memoryCache.Cache.Count;
                mainCace.TotalSizeOfItems = GetApproximateSize();
                mainCace.NumberOfRequests = _memoryCacheSettingVm.Hit + _memoryCacheSettingVm.Miss;
                _context.MemoryCacheSettings.Update(mainCace);
            }
            else
            {
                var cache = new MemoryCacheSettings()
                {
                    Hit = _memoryCacheSettingVm.Hit,
                    Miss = _memoryCacheSettingVm.Miss,
                    TotalItemsNum = _memoryCache.Cache.Count,
                    TotalSizeOfItems = GetApproximateSize(),
                    NumberOfRequests = _memoryCacheSettingVm.Hit + _memoryCacheSettingVm.Miss
                };
                _context.MemoryCacheSettings.Add(cache);
            }
            _context.SaveChanges();
        }
        public long GetApproximateSize()
        {
            var keys = GetListOfKeys();
            //var size =(long) 0.0;
            var size = (long)0.0;
            foreach (var key in keys)
            {
                var value = _memoryCache.Cache.Get<CacheValue>(key: key);
                byte[] bytes = Convert.FromBase64String(value.ImagePath);
                var sizeInBytes = bytes.Count();
                var sizeINMegaByte = sizeInBytes * (0.000001);
                size += (long)sizeINMegaByte;
            }
            return size;
        }
        public List<string> GetListOfKeys()
        {
            var field = typeof(Microsoft.Extensions.Caching.Memory.MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = field.GetValue(_memoryCache.Cache) as ICollection;
            var items = new List<string>();
            if (collection != null)
                foreach (var item in collection)
                {
                    var methodInfo = item.GetType().GetProperty("Key");
                    var val = methodInfo.GetValue(item);
                    items.Add(val.ToString());
                }
            return items;
        }
    }
}
