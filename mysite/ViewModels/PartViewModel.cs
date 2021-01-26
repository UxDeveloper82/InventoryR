﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mysite.ViewModels
{
    public class PartViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        [Required]
        [StringLength(255)]
        public string PartNumber { get; set; }
        [Required]
        [StringLength(255)]
        public string PartDescription { get; set; }
        [Display(Name = "Number in Stock")]
        [Range(1, 20)]
        public byte NumberInStock { get; set; }
        public decimal PartPrice { get; set; }
    }
}
