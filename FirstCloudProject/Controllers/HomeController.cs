using FirstCloudProject.MemoryCacheClasses;
using FirstCloudProject.Models;
using FirstCloudProject.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirstCloudProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly MyMemoryCache _memoryCache;
        private readonly MemoryCacheSettingVm _cacheSetting;
        private readonly ApplicationDbContext _context;


        public HomeController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, MyMemoryCache cache, MemoryCacheSettingVm cacheSetting)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _memoryCache = cache;
            _cacheSetting = cacheSetting;
        }


        public IActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// ///Add Image to db & memory cache :)
        /// </summary>
        /// <returns></returns>**************************************************************
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
                        var stream = new FileStream(serverfolder, FileMode.Create);
                        await model.ImageFile.CopyToAsync(stream);
                        stream.Close();

                        //encrypt the image
                        var img = "E:\\CloudProjects\\FirstProject\\FirstCloudProject\\FirstCloudProject\\wwwroot" + model.ImgURl;
                        byte[] imageArray = System.IO.File.ReadAllBytes(img);
                        String base64ImageRepresentation = Convert.ToBase64String(imageArray);

                        var isExists = _memoryCache.Cache.Get(model.Key);
                        var image = _context.Images.Find(model.Key);
                        var value = new CacheValue()
                        {
                            ImagePath = base64ImageRepresentation,
                            LasModifiedDate = DateTime.Now
                        };
                        var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(10);

                        if (isExists != null || image != null)
                        {
                            if(isExists != null)
                                ViewBag.Action = "UpdateToMem";
                            else
                                ViewBag.Action = "UpdateToDB";
                            image.ImagePath = model.ImgURl;
                            image.LastModifiedDate = DateTime.Now;
                            _context.Images.Update(image);
                            await _context.SaveChangesAsync();
                            if (GetApproximateSize() >= 30)
                            {
                                _memoryCache.Cache.Compact(.50);
                            }
                            _memoryCache.Cache.Set(model.Key, value, cacheEntryOptions);
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
                            if (GetApproximateSize() >= 30)
                            {
                                _memoryCache.Cache.Compact(.50);
                            }
                            _memoryCache.Cache.Set(model.Key, value, cacheEntryOptions);
                            ViewBag.Action = "Add";
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return View();
        }
        /// <summary>
        /// ///Show Image by entering key from cache or db :)
        /// </summary>
        /// <returns></returns>**************************************************************
        [HttpGet]
        public IActionResult ShowImage()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ShowImage(string key)
        {
            try
            {
                CacheValue ImgwithDataObj;
                string a = "";
                //Get it From Meomry Cache
                ImgwithDataObj = _memoryCache.Cache.Get<CacheValue>(key: key);

                if (ImgwithDataObj is null)
                {
                    _cacheSetting.Miss++;
                    ViewBag.Action = "Add";
                    var ImgwithDateFromDB = _context.Images.FirstOrDefault(x => x.Key == key);
                    if (ImgwithDateFromDB == null)
                        return Json(null);
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(10);
                    var img = "E:\\CloudProjects\\FirstProject\\FirstCloudProject\\FirstCloudProject\\wwwroot" + ImgwithDateFromDB.ImagePath;
                    byte[] imageArray = System.IO.File.ReadAllBytes(img);
                    String base64ImageRepresentation = Convert.ToBase64String(imageArray);
                    ImgwithDataObj = new CacheValue
                    {
                        ImagePath = base64ImageRepresentation,
                        LasModifiedDate = DateTime.Now
                    };
                    _memoryCache.Cache.Set(key: key, ImgwithDataObj, cacheEntryOptions);
                     a = ImgwithDateFromDB.ImagePath;
                }
                else
                {
                    _cacheSetting.Hit++;
                    ViewBag.Action = "Update";
                    byte[] bytes = Convert.FromBase64String(ImgwithDataObj.ImagePath);
                    string imageBase64 = Convert.ToBase64String(bytes);
                    string imageSrc = string.Format("data:image/gif;base64,{0}", imageBase64);
                     a = imageSrc;
                }
                var b = ViewBag.Action;
                var c = new DesrlizingObject { Path =a , Action =b };
                return Ok(c);
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// ///Show all images from db :)
        /// </summary>
        /// <returns></returns>**************************************************************
        [HttpGet]
        public IActionResult ShowAllImages()
        {
            var models = _context.Images.ToList();
            return View(models);
        }
        /// <summary>
        /// ///Show all keys from db :)
        /// </summary>
        /// <returns></returns>**************************************************************
        [HttpGet]
        public IActionResult ShowAllKeys()
        {
            _cacheSetting.Miss++;
            var keys = _context.Images.Select(x => x.Key).ToList();
            return View(keys);
        }
        /// <summary>
        /// ///Show keys from cache which are added passed ten minutes :)
        /// </summary>
        /// <returns></returns>**************************************************************
        public IActionResult ShowAllBeforTenMenite()
        {
            var items = GetListOfKeys();
            var date = DateTime.Now;
            var result = new List<DateWithImage>();
            foreach (var key in items)
            {
                var value = _memoryCache.Cache.Get<CacheValue>(key: key);
                _cacheSetting.Hit++;
                var isBefore10Min = (date - value.LasModifiedDate).TotalMinutes <= 10;
                if (isBefore10Min)
                {
                   result.Add(new DateWithImage()
                   {
                       Key= key,
                       Date= value.LasModifiedDate
                   });
                }
            }
            return View(result);
        }
        /// <summary>
        /// ///Show keys from db which are added passed ten minutes :)
        /// </summary>
        /// <returns></returns>**************************************************************
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
            _cacheSetting.Miss++;
            return View(result);
        }
        /// <summary>
        /// ///Settings of memory cache :)
        /// </summary>
        /// <returns></returns>**************************************************************
        public IActionResult SettingsOfMemoryCache (bool hang)
        {
            var items1 = _context.MemoryCacheSettings.ToList();
            if ( items1 != null)
            {
                return View(items1.FirstOrDefault());
            }
            return View();
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
        public long GetApproximateSize()
        {
            var keys = GetListOfKeys();
            //var size =(long) 0.0;
            var size = _cacheSetting.TotalSizeOfItems;
            foreach (var key in keys)
            {
                var value = _memoryCache.Cache.Get<CacheValue>(key: key);
                byte[] bytes = Convert.FromBase64String(value.ImagePath);
                var sizeInBytes=bytes.Count();
                var sizeINMegaByte = sizeInBytes*(0.000001);
                size += (long)sizeINMegaByte;
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

