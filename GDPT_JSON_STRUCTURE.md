# Hướng dẫn cấu trúc File JSON để Import tài liệu GDPT

## 📋 Tổng quan

File JSON **PHẢI** tuân theo cấu trúc nhất định để API có thể parse và import dữ liệu vào database.

## 🔑 Cấu trúc JSON bắt buộc

```json
{
  "topics": [
    {
      "topicName": "string (bắt buộc)",
      "className": "string (bắt buộc)",
      "gradeName": "string (bắt buộc)",
      "strandName": "string (bắt buộc)",
      "objectives": "string (tùy chọn)",
      "source": "string (tùy chọn)",
      "contents": [
        {
          "title": "string (bắt buộc)",
          "summary": "string (tùy chọn)",
          "formulas": [
            {
              "formulaText": "string (bắt buộc)",
              "explanation": "string (tùy chọn)"
            }
          ],
          "examples": [
            {
              "exampleText": "string (bắt buộc)"
            }
          ],
          "media": [
            {
              "type": "string (bắt buộc, thường là 'Image')",
              "url": "string (bắt buộc)",
              "description": "string (tùy chọn)"
            }
          ]
        }
      ]
    }
  ]
}
```

## 📝 Chi tiết từng trường

### Root Level

| Trường   | Kiểu  | Bắt buộc | Mô tả                                    |
| -------- | ----- | -------- | ---------------------------------------- |
| `topics` | Array | ✅       | Danh sách các topics (chủ đề) cần import |

### Topic Object

| Trường       | Kiểu   | Bắt buộc | Mô tả                                                 |
| ------------ | ------ | -------- | ----------------------------------------------------- |
| `topicName`  | String | ✅       | Tên chủ đề (ví dụ: "Phân số")                         |
| `className`  | String | ✅       | Tên lớp (ví dụ: "Lớp 5")                              |
| `gradeName`  | String | ✅       | Tên cấp học (ví dụ: "Cấp 1")                          |
| `strandName` | String | ✅       | Tên mạch kiến thức (ví dụ: "Số, Đại số và Giải tích") |
| `objectives` | String | ❌       | Mục tiêu học tập                                      |
| `source`     | String | ❌       | Nguồn tài liệu (ví dụ: "Thông tư 32/2018")            |
| `contents`   | Array  | ✅       | Danh sách nội dung của topic                          |

### Content Object

| Trường     | Kiểu   | Bắt buộc | Mô tả                                        |
| ---------- | ------ | -------- | -------------------------------------------- |
| `title`    | String | ✅       | Tiêu đề nội dung                             |
| `summary`  | String | ❌       | Tóm tắt nội dung                             |
| `formulas` | Array  | ❌       | Danh sách công thức (có thể để rỗng [])      |
| `examples` | Array  | ❌       | Danh sách ví dụ (có thể để rỗng [])          |
| `media`    | Array  | ❌       | Danh sách hình ảnh/media (có thể để rỗng []) |

### Formula Object

| Trường        | Kiểu   | Bắt buộc | Mô tả                                         |
| ------------- | ------ | -------- | --------------------------------------------- |
| `formulaText` | String | ✅       | Công thức toán học (dùng LaTeX: \\frac{a}{b}) |
| `explanation` | String | ❌       | Giải thích công thức                          |

### Example Object

| Trường        | Kiểu   | Bắt buộc | Mô tả                            |
| ------------- | ------ | -------- | -------------------------------- |
| `exampleText` | String | ✅       | Nội dung ví dụ (có thể có LaTeX) |

### Media Object

| Trường        | Kiểu   | Bắt buộc | Mô tả                          |
| ------------- | ------ | -------- | ------------------------------ |
| `type`        | String | ✅       | Loại media (thường là "Image") |
| `url`         | String | ✅       | Đường dẫn đến hình ảnh (URL)   |
| `description` | String | ❌       | Mô tả hình ảnh                 |

## ✅ Ví dụ JSON tối thiểu (Minimum)

```json
{
  "topics": [
    {
      "topicName": "Phân số",
      "className": "Lớp 5",
      "gradeName": "Cấp 1",
      "strandName": "Số, Đại số và Giải tích",
      "contents": [
        {
          "title": "Khái niệm phân số",
          "formulas": [],
          "examples": [],
          "media": []
        }
      ]
    }
  ]
}
```

## 📚 Ví dụ JSON đầy đủ

Xem file `gdpt-data-sample.json` để xem ví dụ đầy đủ với nhiều topics và contents.

## 🎯 Các mạch kiến thức (Strands) phổ biến

1. **"Số, Đại số và Giải tích"**
2. **"Đo lường và Hình học"**
3. **"Số liệu và Xác suất"**

## 🎓 Các cấp học (Grades) phổ biến

1. **"Cấp 1"** - Tiểu học
2. **"Cấp 2"** - THCS
3. **"Cấp 3"** - THPT

## 📝 Lưu ý quan trọng

1. **Tên trường phân biệt chữ hoa/thường**: Các trường như `topicName`, `className` phải viết đúng camelCase
2. **Các trường bắt buộc không được null**: Nếu không có giá trị, dùng chuỗi rỗng `""` hoặc mảng rỗng `[]`
3. **LaTeX cho công thức toán**: Sử dụng ký hiệu LaTeX cho công thức, ví dụ: `\\frac{a}{b}` (double backslash)
4. **Mảng có thể rỗng**: `formulas`, `examples`, `media` có thể là mảng rỗng `[]` nếu không có dữ liệu
5. **File phải có extension .json**: Khi upload, file phải có đuôi `.json`

## 🔍 Cách test file JSON

1. **Kiểm tra cú pháp JSON**: Dùng online JSON validator
2. **Lấy template mẫu**: Gọi `GET /api/GDPT/template` để xem cấu trúc chính xác
3. **Test với API**: Upload file qua `POST /api/GDPT/import-from-file`

## ❌ Lỗi thường gặp

1. **"File JSON không hợp lệ"**: Kiểm tra cú pháp JSON, dấu phẩy, ngoặc nhọn
2. **"File phải có định dạng JSON"**: Đảm bảo file có đuôi `.json`
3. **"Request không hợp lệ"**: Kiểm tra các trường bắt buộc có đầy đủ không

## 📞 Hỗ trợ

Nếu gặp lỗi, kiểm tra:

- Cấu trúc JSON đúng chưa
- Các trường bắt buộc có đầy đủ chưa
- File có đuôi `.json` chưa
- Authorization token có hợp lệ không (Admin hoặc Teacher)
