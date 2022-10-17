using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCloudProject.Models
{
    public class Image
    {
        [Required]
        [Key]
        public string Key { get; set; }
        [Required]
        public string ImagePath { get; set; }
    
    }
}
