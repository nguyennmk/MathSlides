using MathSlides.Business_Object.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSlides.Repository.Interfaces
{
    /// <summary>
    /// Interface để lấy dữ liệu Topic (thuộc GDPT)
    /// </summary>
    public interface ITopicRepository
    {
        /// <summary>
        /// Lấy thông tin một Topic bằng ID,
        /// bao gồm cả Class và Grade
        /// </summary>
        Task<Topic?> GetByIdAsync(int topicId);
    }
}

