# Hướng dẫn sử dụng API Templates

## 📋 Tổng quan

API Templates cho phép người dùng:

1. **Xem danh sách** tất cả templates có sẵn
2. **Chọn template** theo ID để lấy nội dung JSON mẫu
3. **Sử dụng template** để import dữ liệu GDPT

## 🔗 API Endpoints

### 1. Lấy danh sách templates

**Endpoint:** `GET /api/GDPT/templates`

**Query Parameters:**

- `onlyActive` (optional, default: `true`) - Chỉ lấy templates đang active

**Request:**

```bash
GET /api/GDPT/templates?onlyActive=true
```

**Response 200:**

```json
[
  {
    "templateID": 1,
    "name": "Lý thuyết cơ bản",
    "description": "Template 1 cột, tiêu đề và nội dung",
    "thumbnailUrl": "https://example.com/thumb/template1.png",
    "templateType": "Lý thuyết",
    "tags": "co ban, ly thuyet",
    "isActive": true
  },
  {
    "templateID": 2,
    "name": "Công thức và Ví dụ",
    "description": "Template 2 cột, 1 bên công thức, 1 bên ví dụ",
    "thumbnailUrl": "https://example.com/thumb/template2.png",
    "templateType": "Bài tập",
    "tags": "cong thuc, vi du, 2 cot",
    "isActive": true
  }
]
```

### 2. Lấy template chi tiết theo ID

**Endpoint:** `GET /api/GDPT/templates/{id}`

**Request:**

```bash
GET /api/GDPT/templates/1
```

**Response 200:**

```json
{
  "templateID": 1,
  "name": "Lý thuyết cơ bản",
  "description": "Template 1 cột, tiêu đề và nội dung",
  "thumbnailUrl": "https://example.com/thumb/template1.png",
  "templateType": "Lý thuyết",
  "tags": "co ban, ly thuyet",
  "isActive": true,
  "content": {
    "topics": [
      {
        "topicName": "Phân số",
        "className": "Lớp 5",
        "gradeName": "Cấp 1",
        "strandName": "Số, Đại số và Giải tích",
        "objectives": "Hiểu và thực hiện phép tính với phân số.",
        "source": "Thông tư 32/2018",
        "contents": [
          {
            "title": "Khái niệm phân số",
            "summary": "Phân số là biểu diễn của một phần trong tổng thể.",
            "formulas": [],
            "examples": [],
            "media": []
          }
        ]
      }
    ]
  }
}
```

**Response 404:**

```json
{
  "message": "Template với ID 1 không tồn tại"
}
```

### 3. Lấy template mặc định (tương thích ngược)

**Endpoint:** `GET /api/GDPT/template`

**Request:**

```bash
GET /api/GDPT/template
```

Trả về template mặc định hardcode (không lấy từ database).

## 📝 Cách sử dụng

### Bước 1: Lấy danh sách templates

```javascript
// JavaScript/Fetch
fetch("/api/GDPT/templates")
  .then((response) => response.json())
  .then((templates) => {
    console.log("Danh sách templates:", templates);
    // Hiển thị danh sách cho user chọn
  });
```

### Bước 2: User chọn template

```javascript
// Khi user click vào template có ID = 1
const selectedTemplateId = 1;
```

### Bước 3: Lấy nội dung template

```javascript
fetch(`/api/GDPT/templates/${selectedTemplateId}`)
  .then((response) => response.json())
  .then((template) => {
    console.log("Template detail:", template);
    // Lấy nội dung JSON từ template.content
    const jsonContent = template.content;

    // Cho phép user chỉnh sửa hoặc sử dụng trực tiếp để import
  });
```

### Bước 4: Import dữ liệu từ template

```javascript
// Sử dụng nội dung template để import
fetch("/api/GDPT/import", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify(template.content),
})
  .then((response) => response.json())
  .then((result) => {
    console.log("Import result:", result);
  });
```

## 💡 Ví dụ hoàn chỉnh

```javascript
async function loadAndUseTemplate(templateId) {
  try {
    // 1. Lấy danh sách templates
    const templatesResponse = await fetch("/api/GDPT/templates");
    const templates = await templatesResponse.json();
    console.log("Available templates:", templates);

    // 2. Lấy template chi tiết
    const templateResponse = await fetch(`/api/GDPT/templates/${templateId}`);
    const template = await templateResponse.json();

    if (!template) {
      alert("Template không tồn tại");
      return;
    }

    // 3. Cho user xem preview và chỉnh sửa
    console.log("Template content:", template.content);

    // 4. Import dữ liệu
    const importResponse = await fetch("/api/GDPT/import", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${getToken()}`,
      },
      body: JSON.stringify(template.content),
    });

    const importResult = await importResponse.json();
    console.log("Import completed:", importResult);

    if (importResult.success) {
      alert(
        `Import thành công! ${importResult.totalTopicsImported} topics đã được import.`
      );
    } else {
      alert(`Import thất bại: ${importResult.message}`);
    }
  } catch (error) {
    console.error("Error:", error);
    alert("Có lỗi xảy ra: " + error.message);
  }
}

// Sử dụng
loadAndUseTemplate(1);
```

## 🗄️ Database Schema

Templates được lưu trong bảng `Templates`:

| Cột          | Kiểu           | Mô tả                                   |
| ------------ | -------------- | --------------------------------------- |
| TemplateID   | INT            | ID tự tăng                              |
| Name         | NVARCHAR(100)  | Tên template                            |
| Description  | NVARCHAR(500)  | Mô tả template                          |
| ThumbnailUrl | NVARCHAR(2048) | URL ảnh thumbnail                       |
| TemplatePath | NVARCHAR(2048) | Đường dẫn đến file JSON                 |
| TemplateType | NVARCHAR(50)   | Loại template (Lý thuyết, Bài tập, ...) |
| Tags         | NVARCHAR(255)  | Tags phân loại                          |
| IsActive     | BIT            | Template có đang active không           |

## 📦 Tạo Templates trong Database

Chạy SQL script để thêm templates mẫu:

```sql
INSERT INTO Templates (Name, Description, ThumbnailUrl, TemplatePath, TemplateType, Tags, IsActive)
VALUES
(N'Lý thuyết cơ bản',
 N'Template 1 cột, tiêu đề và nội dung',
 'https://example.com/thumb/template1.png',
 '/templates/theory-basic.json',
 'Lý thuyết',
 'co ban, ly thuyet',
 1),
(N'Công thức và Ví dụ',
 N'Template 2 cột, 1 bên công thức, 1 bên ví dụ',
 'https://example.com/thumb/template2.png',
 '/templates/formula-example.json',
 'Bài tập',
 'cong thuc, vi du, 2 cot',
 1);
```

## 📁 Lưu trữ Template Files

Templates có thể được lưu trữ theo 2 cách:

### Cách 1: Lưu trong file system

- Đặt file JSON trong thư mục `/templates/`
- Cập nhật `TemplatePath` trong database trỏ đến file

### Cách 2: Lưu trực tiếp trong database (cần mở rộng)

- Có thể thêm cột `TemplateContent` kiểu NVARCHAR(MAX) để lưu JSON trực tiếp
- Cập nhật code để đọc từ cột này thay vì file

## 🔒 Authentication

- **Public endpoints**: Tất cả template endpoints đều là `[AllowAnonymous]` - không cần authentication
- **Import endpoints**: Cần authentication với role Admin hoặc Teacher

## 🐛 Xử lý lỗi

### Template không tồn tại

```json
{
  "message": "Template với ID {id} không tồn tại"
}
```

### Template file không tìm thấy

- Nếu `TemplatePath` trỏ đến file không tồn tại, API sẽ trả về `content: null`
- Đảm bảo file JSON tồn tại tại đường dẫn đã chỉ định

### Template JSON không hợp lệ

- Nếu file JSON không parse được, `content` sẽ là `null`
- Kiểm tra lại format JSON trong file template
