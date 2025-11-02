# Hướng Dẫn Sử Dụng API Import PowerPoint

## Endpoint: `/api/Powerpoint/import`

### Mô tả

API này cho phép import file PowerPoint (.pptx hoặc .ppt) và chuyển đổi sang định dạng JSON.

### Thông tin Endpoint

- **Method**: `POST`
- **URL**: `/api/Powerpoint/import`
- **Content-Type**: `multipart/form-data`
- **Authentication**: Không yêu cầu (`[AllowAnonymous]`)
- **Max File Size**: 50MB

### Request Parameters

| Tham số       | Loại      | Bắt buộc | Mô tả                             |
| ------------- | --------- | -------- | --------------------------------- |
| `File`        | IFormFile | ✅ Có    | File PowerPoint (.pptx hoặc .ppt) |
| `Name`        | string    | ❌ Không | Tên của template (tùy chọn)       |
| `Description` | string    | ❌ Không | Mô tả của template (tùy chọn)     |

### Response

**Success (200 OK):**

```json
{
  "success": true,
  "message": "Import thành công",
  "data": {
    "templatePath": "path/to/saved/file.pptx",
    "fileName": "example.pptx",
    "slideCount": 5,
    "jsonContent": "{ ... JSON content ... }"
  }
}
```

**Error (400 Bad Request):**

```json
{
  "message": "File không được để trống"
}
```

hoặc

```json
{
  "message": "Chỉ chấp nhận file PowerPoint (.pptx, .ppt)"
}
```

**Error (500 Internal Server Error):**

```json
{
  "message": "Lỗi khi xử lý file PowerPoint",
  "error": "Chi tiết lỗi..."
}
```

---

## Các Cách Sử Dụng

### 1. Sử dụng cURL

#### Chỉ upload file:

```bash
curl -X POST "https://localhost:5258/api/Powerpoint/import" \
  -F "File=@/path/to/your/file.pptx"
```

#### Upload file với Name và Description:

```bash
curl -X POST "https://localhost:5258/api/Powerpoint/import" \
  -F "File=@/path/to/your/file.pptx" \
  -F "Name=Template Toán Học" \
  -F "Description=Mẫu slide cho bài giảng toán"
```

### 2. Sử dụng Postman

1. **Method**: Chọn `POST`
2. **URL**: `https://localhost:5258/api/Powerpoint/import`
3. **Body**: Chọn `form-data`
4. **Thêm các fields**:
   - Key: `File` (type: File), Value: Chọn file PowerPoint
   - Key: `Name` (type: Text), Value: Tên template (tùy chọn)
   - Key: `Description` (type: Text), Value: Mô tả (tùy chọn)
5. Click **Send**

### 3. Sử dụng JavaScript (Fetch API)

```javascript
async function importPowerpoint(file, name, description) {
  const formData = new FormData();
  formData.append("File", file);

  if (name) {
    formData.append("Name", name);
  }

  if (description) {
    formData.append("Description", description);
  }

  try {
    const response = await fetch(
      "https://localhost:5258/api/Powerpoint/import",
      {
        method: "POST",
        body: formData,
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || "Lỗi khi import file");
    }

    const result = await response.json();
    console.log("Import thành công:", result);
    return result;
  } catch (error) {
    console.error("Lỗi:", error);
    throw error;
  }
}

// Sử dụng với HTML file input
document.getElementById("fileInput").addEventListener("change", async (e) => {
  const file = e.target.files[0];
  if (file) {
    try {
      const result = await importPowerpoint(
        file,
        "Template mới",
        "Mô tả template"
      );
      console.log("Template Path:", result.data.templatePath);
      console.log("Slide Count:", result.data.slideCount);
      console.log("JSON Content:", result.data.jsonContent);
    } catch (error) {
      alert("Lỗi: " + error.message);
    }
  }
});
```

### 4. Sử dụng C# HttpClient

```csharp
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

public class PowerpointImportClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public PowerpointImportClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<ImportResponse> ImportPowerpointAsync(
        string filePath,
        string? name = null,
        string? description = null)
    {
        using var formData = new MultipartFormDataContent();

        // Đọc file và thêm vào form
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.presentationml.presentation");
        formData.Add(fileContent, "File", Path.GetFileName(filePath));

        // Thêm Name và Description nếu có
        if (!string.IsNullOrEmpty(name))
        {
            formData.Add(new StringContent(name), "Name");
        }

        if (!string.IsNullOrEmpty(description))
        {
            formData.Add(new StringContent(description), "Description");
        }

        // Gửi request
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/Powerpoint/import", formData);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ImportResponse>();
    }
}

// Model cho response
public class ImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ImportData Data { get; set; }
}

public class ImportData
{
    public string TemplatePath { get; set; }
    public string FileName { get; set; }
    public int SlideCount { get; set; }
    public string JsonContent { get; set; }
}

// Sử dụng:
var client = new HttpClient();
var importClient = new PowerpointImportClient(client, "https://localhost:5258");

var result = await importClient.ImportPowerpointAsync(
    @"C:\path\to\file.pptx",
    "Template Toán Học",
    "Mô tả template"
);
```

### 5. Sử dụng Swagger UI

1. Mở trình duyệt và truy cập: `https://localhost:5258/swagger`
2. Tìm endpoint `POST /api/Powerpoint/import`
3. Click **Try it out**
4. Trong phần Request body:
   - Click **Choose File** để chọn file PowerPoint
   - Nhập `Name` (tùy chọn)
   - Nhập `Description` (tùy chọn)
5. Click **Execute**
6. Xem kết quả trong phần **Responses**

---

## Ví dụ Request/Response

### Request

```
POST /api/Powerpoint/import HTTP/1.1
Host: localhost:5258
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="File"; filename="example.pptx"
Content-Type: application/vnd.openxmlformats-officedocument.presentationml.presentation

[Binary file content]
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="Name"

Template Toán Học Lớp 5
------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="Description"

Mẫu slide cho bài giảng toán lớp 5
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

### Response Success

```json
{
  "success": true,
  "message": "Import thành công",
  "data": {
    "templatePath": "Templates/example_20241201_123456.pptx",
    "fileName": "example.pptx",
    "slideCount": 10,
    "jsonContent": "{\"slides\":[...],\"metadata\":{...}}"
  }
}
```

---

## Lưu Ý

1. **Định dạng file**: Chỉ chấp nhận `.pptx` và `.ppt`
2. **Kích thước file**: Tối đa 50MB
3. **Lưu trữ**: File sẽ được lưu vào thư mục `wwwroot/Templates/`
4. **JSON Response**: `jsonContent` chứa toàn bộ nội dung của PowerPoint đã được chuyển đổi sang JSON
5. **Template Path**: Sau khi import, bạn có thể sử dụng `templatePath` để link với Template ID bằng endpoint `/api/Powerpoint/link-template`

---

## Endpoint Liên Quan

### 1. Lấy thông tin PowerPoint đã import (GET)

Sau khi import, bạn có thể lấy lại thông tin từ file JSON đã lưu:

**Endpoint**: `GET /api/Powerpoint/info`

**Query Parameter**:

- `templatePath` (bắt buộc): Đường dẫn file JSON (ví dụ: `/Templates/CP4_20251102190631.json`)

**Ví dụ sử dụng:**

#### cURL:

```bash
curl -X GET "http://localhost:5258/api/Powerpoint/info?templatePath=/Templates/CP4_20251102190631.json"
```

#### JavaScript:

```javascript
async function getPowerpointInfo(templatePath) {
  const response = await fetch(
    `http://localhost:5258/api/Powerpoint/info?templatePath=${encodeURIComponent(
      templatePath
    )}`
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || "Lỗi khi lấy thông tin");
  }

  return await response.json();
}

// Sử dụng
const result = await getPowerpointInfo("/Templates/CP4_20251102190631.json");
console.log("Slide Count:", result.data.slideCount);
console.log("JSON Content:", result.data.jsonContent);
```

#### C#:

```csharp
public async Task<ImportResponse> GetPowerpointInfoAsync(string templatePath)
{
    var encodedPath = Uri.EscapeDataString(templatePath);
    var response = await _httpClient.GetAsync($"{_baseUrl}/api/Powerpoint/info?templatePath={encodedPath}");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<ImportResponse>();
}
```

**Response Success (200 OK):**

```json
{
  "success": true,
  "message": "Đã tải thông tin PowerPoint thành công. Tìm thấy 5 slide(s).",
  "data": {
    "templatePath": "/Templates/CP4_20251102190631.json",
    "fileName": "CP4_20251102190631.json",
    "slideCount": 5,
    "jsonContent": "{ ... JSON content ... }"
  }
}
```

**Response Error (404 Not Found):**

```json
{
  "message": "File không tồn tại: /Templates/invalid.json"
}
```

---

### 2. Cập nhật nội dung JSON của file PowerPoint (PUT)

Cập nhật nội dung JSON của file đã import:

**Endpoint**: `PUT /api/Powerpoint/update`

**Request Body:**

```json
{
  "templatePath": "/Templates/CP4_20251102190631.json",
  "jsonContent": "{ ... JSON content mới ... }"
}
```

**Ví dụ sử dụng:**

#### cURL:

```bash
curl -X PUT "http://localhost:5258/api/Powerpoint/update" \
  -H "Content-Type: application/json" \
  -d '{
    "templatePath": "/Templates/CP4_20251102190631.json",
    "jsonContent": "{\"SlideCount\": 5, \"Slides\": [...]}"
  }'
```

#### JavaScript:

```javascript
async function updatePowerpointInfo(templatePath, jsonContent) {
  const response = await fetch("http://localhost:5258/api/Powerpoint/update", {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      templatePath: templatePath,
      jsonContent: jsonContent,
    }),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || "Lỗi khi cập nhật");
  }

  return await response.json();
}

// Sử dụng
const updatedJson = JSON.stringify({
  SlideCount: 5,
  Slides: [
    {
      SlideId: "rId6",
      Shapes: [],
      Texts: [],
    },
  ],
  CreatedAt: new Date().toISOString(),
});

const result = await updatePowerpointInfo(
  "/Templates/CP4_20251102190631.json",
  updatedJson
);
```

#### C#:

```csharp
public async Task<ImportResponse> UpdatePowerpointInfoAsync(
    string templatePath,
    string jsonContent)
{
    var requestBody = new
    {
        templatePath = templatePath,
        jsonContent = jsonContent
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PutAsync($"{_baseUrl}/api/Powerpoint/update", content);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<ImportResponse>();
}
```

**Response Success (200 OK):**

```json
{
  "success": true,
  "message": "Đã cập nhật file PowerPoint thành công. Tìm thấy 5 slide(s).",
  "data": {
    "templatePath": "/Templates/CP4_20251102190631.json",
    "fileName": "CP4_20251102190631.json",
    "slideCount": 5,
    "jsonContent": "{ ... JSON content đã cập nhật ... }"
  }
}
```

**Response Error (400 Bad Request):**

```json
{
  "message": "jsonContent không được để trống"
}
```

hoặc

```json
{
  "message": "JSON content không hợp lệ: ..."
}
```

**Response Error (404 Not Found):**

```json
{
  "message": "File không tồn tại: /Templates/invalid.json"
}
```

---

### 3. Link Template Path với Template ID

Sau khi import, bạn có thể link `templatePath` với một Template ID:

```
POST /api/Powerpoint/link-template
Content-Type: application/json

{
  "templateID": 1,
  "templatePath": "Templates/example_20241201_123456.pptx"
}
```
