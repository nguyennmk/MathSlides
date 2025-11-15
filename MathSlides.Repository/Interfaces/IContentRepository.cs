using MathSlides.Business_Object.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Repository.Interfaces
{
    public interface IContentRepository
    {
        Task<List<Content>> CreateBulkContentAsync(List<Content> contents);
        Task<List<Content>> GetContentsByTopicIdAsync(int topicId);
    }
}
