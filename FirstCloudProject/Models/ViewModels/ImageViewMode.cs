using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCloudProject.Models.ViewModels
{
    public class ImageViewMode
    {
        [Required]
        public string Key { get; set; }
        [Required]
        public IFormFile ImageFile { get; set; }
        public string ImgURl { get; set; }
    }
}
