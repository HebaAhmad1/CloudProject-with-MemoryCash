﻿using FirstCloudProject.Models;
using FirstCloudProject.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace FirstCloudProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        
        // ObjectCache _memoryCache = MemoryCache.Default;
        private readonly IMemoryCache _memoryCache;

         int hit;
         int miss;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _memoryCache = memoryCache;
        }

        public IActionResult Index()
        {
             return View();
        }
        [HttpGet]
        public IActionResult AddImage()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddImage(ImageViewMode model)
        {
            if (ModelState.IsValid)
            {
                if (model.ImageFile != null)
                {
                    try
                    {
                        string folder = "Images/";
                        folder += Guid.NewGuid() + "_" + model.ImageFile.FileName;
                        model.ImgURl = $"/{folder}";
                        string serverfolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);
                        //then we move this coverimg to folder in myproject
                        await model.ImageFile.CopyToAsync(new FileStream(serverfolder, FileMode.Create));

                        var isExists = _memoryCache.Get(model.Key);
                        var exitimage = _context.Images.Find(model.Key);
                        if (isExists != null || exitimage != null)
                        {
                            var image = _context.Images.FirstOrDefault(x => x.Key == model.Key);
                            image.ImagePath = model.ImgURl;
                            image.LastModifiedDate = DateTime.Now;
                            _context.Images.Update(image);
                            await _context.SaveChangesAsync();
                            miss++;
                            var value = new CacheValue()
                            {
                                ImagePath = model.ImgURl,
                                LasModifiedDate=DateTime.Now
                            };
                            _memoryCache.Set(model.Key, value);
                            hit++;
                            //var list = new List<String>() {model.ImgURl, DateTime.Now.ToString() };
                            //var value = String.Join(",", list);
                            //HttpContext.Response.Cookies.Append(model.Key, value);
                        }
                        else
                        {
                            var imageDb = new Image()
                            {
                                Key = model.Key,
                                ImagePath = model.ImgURl
                            };
                            await _context.Images.AddAsync(imageDb);
                            await _context.SaveChangesAsync();
                            miss++;
                            var value = new CacheValue()
                            {
                                ImagePath = model.ImgURl,
                                LasModifiedDate = DateTime.Now
                            };
                            var cacheItemPolicy = new CacheItemPolicy
                            {
                                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(60.0)
                            };
                            _memoryCache.Set(model.Key, value);
                            hit++;
                            //var list = new List<String>() { model.ImgURl, DateTime.Now.ToString() };
                            //var value = String.Join(",", list);
                            //HttpContext.Response.Cookies.Append(model.Key, value);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return View();
        }
        [HttpGet]
        public IActionResult ShowImage()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ShowImage(string key)
        {
            CacheValue ImgwithDataObj;
            //Get it From Meomry Cache
            ImgwithDataObj = _memoryCache.Get<CacheValue>(key: key);

            if (ImgwithDataObj is null)
            {
                miss++;
                var ImgwithDateFromDB = _context.Images.FirstOrDefault(x => x.Key == key);
                if (ImgwithDateFromDB == null)
                    return Json(null);
                ImgwithDataObj = new CacheValue
                {
                    ImagePath = ImgwithDateFromDB.ImagePath,
                    LasModifiedDate = DateTime.Now
                };
                _memoryCache.Set(key: key, ImgwithDataObj);
            }
            else
                hit++;
            return Json(ImgwithDataObj.ImagePath);
        }
        [HttpGet]
        public IActionResult ShowAllImages()
        {
            var models = _context.Images.ToList();
            return View(models);
        }
        [HttpGet]
        public IActionResult ShowAllKeys()
        {
            miss++;
            var keys = _context.Images.Select(x => x.Key).ToList();
            return View(keys);
        }

        public IActionResult ShowAllBeforTenMenite()
        {
            var items = GetListOfKeys();
            var date = DateTime.Now;
            var result = new List<DateWithImage>();
            foreach (var key in items)
            {
                var value = _memoryCache.Get<CacheValue>(key: key);
                hit++;
                    var isBefore10Min = (date - value.LasModifiedDate).TotalMinutes <= 10;
                    if (isBefore10Min)
                    {
                        result.Add(new DateWithImage()
                        {
                            Key= key,
                            Date= value.LasModifiedDate
                        });
                    }
                //}
            }
            return View(result);
        }
        public IActionResult ShowAllKeysBeforTenMeniteFromDb()
        {
        
             var date = DateTime.Now;
            var images = _context.Images.ToList();
            var result = new List<DateWithImage>();
            foreach (var image in images)
            {
                var isNew = (date - image.LastModifiedDate ).TotalMinutes <= 10;
                if (isNew)
                {
                    result.Add(new DateWithImage()
                    {
                        Key = image.Key,
                        Date = image.LastModifiedDate
                    });
                }
            }
            return View(result);
        }
        public IActionResult SettingsOfMemoryCache()
        {
            var items = GetListOfKeys();
            var item = new MemoryCacheSettings()
            {
                Capacity=30,
                Hit=hit,
                Miss=miss,
                TotalItemsNum=items.Count,
                TotalSizeOfItems= GetApproximateSize(),
            };
            return View();
        }
        public List<string> GetListOfKeys()
        {
            var field = typeof(Microsoft.Extensions.Caching.Memory.MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = field.GetValue(_memoryCache) as ICollection;
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
        public long GetApproximateSize()
        {
            var keys = GetListOfKeys();
            var size =(long) 0.0;
           foreach (var key in keys)
            {
                var value = _memoryCache.Get<CacheValue>(key: key);
                var img = "F:\\cloud\\CloudProject-with-MemoryCash\\FirstCloudProject\\wwwroot" + value.ImagePath;
                FileInfo fileInfo = new FileInfo(img);
                long fileSizekB = fileInfo.Length / (1000);
                long fileSizeMB = fileSizekB / (1024);
                size += fileSizeMB;
              
            }
            return size;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

