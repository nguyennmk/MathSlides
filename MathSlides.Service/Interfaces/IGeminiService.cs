using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Service.Interfaces
{
    public interface IGeminiService
    {
        Task<string> GenerateContentAsync(string prompt);
    }
}
