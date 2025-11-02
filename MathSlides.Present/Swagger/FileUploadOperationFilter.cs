using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace MathSlides.Present.Swagger
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Tìm tất cả parameters có [FromForm]
            var allFormParameters = context.MethodInfo.GetParameters()
                .Where(p => p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any())
                .ToList();

            if (!allFormParameters.Any())
                return;

            var formFileParameters = allFormParameters
                .Where(p => p.ParameterType == typeof(IFormFile) || 
                           p.ParameterType == typeof(IFormFile[]) ||
                           (p.ParameterType.IsGenericType && 
                            p.ParameterType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>) &&
                            p.ParameterType.GetGenericArguments()[0] == typeof(IFormFile)))
                .ToList();

            if (formFileParameters.Any())
            {
                var properties = new Dictionary<string, OpenApiSchema>();
                
                // Thêm file parameters
                foreach (var param in formFileParameters)
                {
                    properties[param.Name ?? "file"] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "File to upload"
                    };
                }

                // Thêm các string parameters từ [FromForm]
                var stringParameters = context.MethodInfo.GetParameters()
                    .Where(p => p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any() &&
                               p.ParameterType == typeof(string))
                    .ToList();

                foreach (var param in stringParameters)
                {
                    properties[param.Name ?? "value"] = new OpenApiSchema
                    {
                        Type = "string"
                    };
                }

                // Xóa tất cả các parameters có [FromForm] trước
                if (operation.Parameters != null)
                {
                    var allFormParams = formFileParameters.Concat(stringParameters).ToList();
                    var parametersToRemove = operation.Parameters
                        .Where(p => allFormParams.Any(fp => fp.Name == p.Name))
                        .ToList();
                    
                    foreach (var paramToRemove in parametersToRemove)
                    {
                        operation.Parameters.Remove(paramToRemove);
                    }
                }

                // Sau đó thêm RequestBody
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = properties
                            }
                        }
                    }
                };
            }
        }
    }
}

