using FirstCloudProject.Models;
using FirstCloudProject.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCloudProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
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

                        var cookie = Request.Cookies[model.Key];
                        var exitimage = _context.Images.Find(model.Key);
                        if (cookie != null || exitimage != null)
                        {
                            var list = new List<String>() {model.ImgURl, DateTime.Now.ToString() };
                            var value = String.Join(",", list);
                            var image = _context.Images.FirstOrDefault(x => x.Key == model.Key);
                            image.ImagePath = model.ImgURl;
                            _context.Images.Update(image);
                            await _context.SaveChangesAsync();
                            HttpContext.Response.Cookies.Append(model.Key, value);
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
                            var list = new List<String>() { model.ImgURl, DateTime.Now.ToString() };
                            var value = String.Join(",", list);
                            HttpContext.Response.Cookies.Append(model.Key, value);
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
            var imagePath = _context.Images.FirstOrDefault(x => x.Key == key);
            if (imagePath != null)
            {
                return Json(imagePath.ImagePath);
            }
            return Json(null);
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
            var keys = _context.Images.Select(x => x.Key).ToList();
            return View(keys);
        }

        public IActionResult ShowAllBeforTenMenite()
        {
            var keys = HttpContext.Request.Cookies.Keys.ToList();
            var date = DateTime.Now;
            var result = new List<DateWithImage>();
            foreach (var key in keys)
            {
                var value=HttpContext.Request.Cookies[key].Split(',').ToList();
                if(value.Count > 1)
                {
                    var isnew = (Convert.ToDateTime(value[1]) - date).TotalMinutes <= 10;
                    if (isnew)
                    {
                        result.Add(new DateWithImage()
                        {
                            Key= key,
                            Date= Convert.ToDateTime(value[1])
                        });
                    }
                }
            }
            return View(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

