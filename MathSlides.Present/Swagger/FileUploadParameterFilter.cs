using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace MathSlides.Present.Swagger
{
    public class FileUploadParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            var paramInfo = context.ParameterInfo;
            
            // Ignore parameters có [FromForm] - sẽ được xử lý bởi RequestBody trong OperationFilter
            if (paramInfo != null && 
                paramInfo.GetCustomAttributes(typeof(FromFormAttribute), false).Any())
            {
                // Đánh dấu parameter này để OperationFilter có thể xử lý
                // Không remove schema ngay, mà để OperationFilter xử lý sau
                if (paramInfo.ParameterType == typeof(IFormFile) || 
                    paramInfo.ParameterType == typeof(IFormFile[]) ||
                    (paramInfo.ParameterType.IsGenericType && 
                     paramInfo.ParameterType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>) &&
                     paramInfo.ParameterType.GetGenericArguments()[0] == typeof(IFormFile)))
                {
                    // Map IFormFile to binary schema để tránh lỗi
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }
            }
        }
    }
}

