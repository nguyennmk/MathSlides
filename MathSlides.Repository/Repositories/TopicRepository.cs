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
    public class TopicRepository : ITopicRepository
    {
        private readonly MathSlidesAuthDbContext _context;

        public TopicRepository(MathSlidesAuthDbContext context)
        {
            _context = context;
        }

        public async Task<Topic?> GetByIdAsync(int topicId)
        {
            return await _context.Topics
                .Include(t => t.Class)
                .ThenInclude(c => c.Grade) 
                .Include(t => t.Strand)
                .FirstOrDefaultAsync(t => t.TopicID == topicId);
        }
    }
}
