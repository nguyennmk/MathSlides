-- Script để insert các templates mẫu vào database
-- Chạy script này sau khi đã tạo database và bảng Templates

USE MathSlidesDB;
GO

-- Xóa dữ liệu cũ nếu có (optional)
-- DELETE FROM Templates;

-- Insert templates mẫu
INSERT INTO Templates (Name, Description, ThumbnailUrl, TemplatePath, TemplateType, Tags, IsActive)
VALUES
-- Template 1: Lý thuyết cơ bản
(N'Template Phân số - Lý thuyết', 
 N'Template mẫu cho bài học về phân số, bao gồm khái niệm và ví dụ cơ bản', 
 'https://via.placeholder.com/300x200?text=Phan+So+Ly+Thuyet', 
 'C:\Templates\phan-so-ly-thuyet.json',  -- Thay đổi đường dẫn phù hợp
 'Lý thuyết', 
 N'phan so, toan hoc, lop 5, cap 1', 
 1),

-- Template 2: Công thức và Ví dụ
(N'Template Phân số - Công thức', 
 N'Template mẫu cho bài học về công thức tính toán với phân số, kèm ví dụ minh họa', 
 'https://via.placeholder.com/300x200?text=Cong+Thuc+Phan+So', 
 'C:\Templates\phan-so-cong-thuc.json',  -- Thay đổi đường dẫn phù hợp
 'Công thức', 
 N'phan so, cong thuc, toan hoc, lop 5', 
 1),

-- Template 3: Hình học
(N'Template Hình tam giác', 
 N'Template mẫu cho bài học về các loại tam giác và cách tính diện tích', 
 'https://via.placeholder.com/300x200?text=Hinh+Tam+Giac', 
 'C:\Templates\hinh-tam-giac.json',  -- Thay đổi đường dẫn phù hợp
 'Hình học', 
 N'hinh hoc, tam giac, lop 6, cap 2', 
 1),

-- Template 4: Template tối thiểu
(N'Template Tối thiểu', 
 N'Template mẫu tối thiểu với cấu trúc cơ bản nhất, phù hợp để bắt đầu', 
 'https://via.placeholder.com/300x200?text=Template+Minimal', 
 'C:\Templates\template-minimal.json',  -- Thay đổi đường dẫn phù hợp
 'Cơ bản', 
 N'co ban, mau', 
 1);
GO

-- Kiểm tra kết quả
SELECT * FROM Templates;
GO

-- Lưu ý:
-- 1. Thay đổi TemplatePath phù hợp với thư mục trên server của bạn
-- 2. Tạo các file JSON tương ứng tại các đường dẫn đã chỉ định
-- 3. Đảm bảo file JSON có format đúng theo cấu trúc ImportGDPTRequest

