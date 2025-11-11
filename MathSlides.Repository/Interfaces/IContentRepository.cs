using MathSlides.Business_Object.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Repository.Interfaces
{
    /// <summary>
    /// Interface cho nghiệp vụ CRUD trên Content và các entities con
    /// </summary>
    public interface IContentRepository
    {
        /// <summary>
        /// Tạo một danh sách Content (và các Formulas, Examples con) cho một Topic
        /// </summary>
        Task<List<Content>> CreateBulkContentAsync(List<Content> contents);

        /// <summary>
        /// Lấy tất cả Content (kèm Formulas, Examples) của một Topic
        /// </summary>
        Task<List<Content>> GetContentsByTopicIdAsync(int topicId);
    }
}
