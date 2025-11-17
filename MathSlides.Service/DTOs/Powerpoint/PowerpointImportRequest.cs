using Microsoft.AspNetCore.Http;

namespace MathSlides.Business_Object.Models.DTOs.Powerpoint
{
    public class PowerpointImportRequest
    {
        public IFormFile File { get; set; } = default!;
        public string? Name { get; set; }
        public string? Description { get; set; }

        public string? Tags { get; set; }

        public IFormFile? ThumbnailImage { get; set; } 
    }
}

