using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.DTOs.GDPT
{
    public class UpdateTopicRequestDTO
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int ClassID { get; set; }

        [Required]
        public int StrandID { get; set; }

        public string? Objectives { get; set; }

        [StringLength(255)]
        public string? Source { get; set; }
    }
}
