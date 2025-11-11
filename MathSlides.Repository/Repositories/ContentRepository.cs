using MathSlides.Business_Object.Models.Entities;
using MathSlides.Data_Analysis_Object;
using MathSlides.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Repository.Repositories
{
    /// <summary>
    /// Triển khai nghiệp vụ CRUD cho Content
    /// </summary>
    public class ContentRepository : IContentRepository
    {
        // Dùng AuthDbContext như file GDPTRepository của bạn
        private readonly MathSlidesAuthDbContext _context;

        public ContentRepository(MathSlidesAuthDbContext context)
        {
            _context = context;
        }

        public async Task<List<Content>> CreateBulkContentAsync(List<Content> contents)
        {
            // 'contents' đã chứa Formulas và Examples
            await _context.Contents.AddRangeAsync(contents);
            await _context.SaveChangesAsync();
            return contents;
        }

        public async Task<List<Content>> GetContentsByTopicIdAsync(int topicId)
        {
            return await _context.Contents
                .Where(c => c.TopicID == topicId)
                .Include(c => c.Formulas)
                .Include(c => c.Examples)
                .Include(c => c.Media)
                .ToListAsync();
        }
    }
}
