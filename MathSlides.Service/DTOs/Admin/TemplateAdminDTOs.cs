using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.DTOs.Admin
{
    public class CreateTemplateRequestDTO
    {
        [Required]
        public IFormFile PptxFile { get; set; } = null!; // File .pptx

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public IFormFile ThumbnailFile { get; set; } = null;

        [StringLength(50)]
        public string? TemplateType { get; set; }

        [StringLength(255)]
        public string? Tags { get; set; }
    }

    public class UpdateTemplateRequestDTO
    {
        public IFormFile? PptxFile { get; set; } // File .pptx (TÙY CHỌN, nếu null là không thay đổi file)
        public IFormFile? ThumbnailFile { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? TemplateType { get; set; }

        [StringLength(255)]
        public string? Tags { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}
