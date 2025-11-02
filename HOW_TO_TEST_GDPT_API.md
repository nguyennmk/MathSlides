# Hướng dẫn Test API Import GDPT

## 🔐 Vấn đề 401 Unauthorized

Nếu bạn gặp lỗi **401 Unauthorized** khi gọi API `/api/GDPT/import-from-file`, hãy kiểm tra các điểm sau:

## ✅ Checklist Kiểm tra

### 1. Token có hợp lệ không?

**Kiểm tra token:**

- Token không được null hoặc rỗng
- Token chưa hết hạn (check field `expiration` trong response login)
- Token phải được decode thành công

**Kiểm tra expiration:**

```json
{
  "expiration": "2025-11-02T05:40:12Z" // Đảm bảo thời gian này chưa qua
}
```

### 2. Cách gửi Authorization Header

**⚠️ QUAN TRỌNG:** Phải gửi header đúng format:

```
Authorization: Bearer {token}
```

**Ví dụ:**

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**❌ SAI:**

- `Authorization: {token}` (thiếu "Bearer ")
- `Authorization: bearer {token}` (sai chữ hoa)
- `Token: {token}` (sai tên header)

### 3. Test với Postman

**Cách test đúng:**

1. **Tạo Request:**

   - Method: `POST`
   - URL: `https://your-api/api/GDPT/import-from-file`

2. **Headers:**

   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiVmlldEx1YW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJMdWFudG1zZTE3MjQ5M0BmcHQuZWR1LnZuIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJqdGkiOiJjOTY0ZTZlMS0zYTk4LTQ4NGQtYmFmNC1hNWIzYTk2ZjFkMDUiLCJpYXQiOjE3NjIwNTg0MTIsImV4cCI6MTc2MjA2MjAxMiwiaXNzIjoiTWF0aFNsaWRlc0F1dGhBUEkiLCJhdWQiOiJNYXRoU2xpZGVzQXV0aENsaWVudCJ9.5x7WwQDVmku7lYMaRm4Zb6QD141UFYrgUYMlPAy7VkU
   ```

3. **Body:**
   - Chọn tab: `form-data` hoặc `binary`
   - Key: `file` (type: File)
   - Value: Chọn file JSON (ví dụ: `gdpt-data-sample.json`)

### 4. Test với cURL

```bash
curl -X POST "https://your-api/api/GDPT/import-from-file" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -F "file=@gdpt-data-sample.json"
```

### 5. Test với JavaScript/Fetch

```javascript
const formData = new FormData();
formData.append("file", fileInput.files[0]); // fileInput là input[type="file"]

fetch("/api/GDPT/import-from-file", {
  method: "POST",
  headers: {
    Authorization: `Bearer ${token}`, // token từ login response
  },
  body: formData,
})
  .then((response) => {
    if (response.status === 401) {
      console.error("Unauthorized - Token không hợp lệ hoặc đã hết hạn");
    }
    return response.json();
  })
  .then((data) => console.log(data))
  .catch((error) => console.error("Error:", error));
```

### 6. Test với C# HttpClient

```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

var formData = new MultipartFormDataContent();
var fileContent = new ByteArrayContent(File.ReadAllBytes("gdpt-data-sample.json"));
fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
formData.Add(fileContent, "file", "gdpt-data-sample.json");

var response = await httpClient.PostAsync("https://your-api/api/GDPT/import-from-file", formData);
var result = await response.Content.ReadAsStringAsync();
```

## 🔍 Debug Steps

### Bước 1: Kiểm tra Token có hợp lệ không

Gọi API `/api/Auth/profile` để test token:

```bash
curl -X GET "https://your-api/api/Auth/profile" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Nếu trả về 401:** Token không hợp lệ → Đăng nhập lại để lấy token mới

**Nếu trả về 200:** Token hợp lệ → Tiếp tục bước 2

### Bước 2: Kiểm tra Role

Response từ `/api/Auth/profile` phải có role là **"Admin"** hoặc **"Teacher"**:

```json
{
  "username": "VietLuan",
  "role": "Admin" // ✅ Phải là Admin hoặc Teacher
}
```

### Bước 3: Kiểm tra Token trong JWT

Decode token tại [jwt.io](https://jwt.io) và kiểm tra:

1. **Payload phải có:**

```json
{
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin",
  "iss": "MathSlidesAuthAPI",
  "aud": "MathSlidesAuthClient"
}
```

2. **Token chưa hết hạn:**

- Check field `exp` trong payload
- So sánh với thời gian hiện tại

### Bước 4: Kiểm tra CORS (nếu gọi từ browser)

Nếu gọi từ frontend, có thể cần cấu hình CORS trong `Program.cs`.

## 🐛 Các lỗi thường gặp

### Lỗi 1: "401 Unauthorized" - Token không được gửi

**Nguyên nhân:** Header Authorization không được gửi
**Giải pháp:** Kiểm tra lại cách gửi header

### Lỗi 2: "401 Unauthorized" - Token hết hạn

**Nguyên nhân:** Token đã quá thời gian expiration
**Giải pháp:** Đăng nhập lại để lấy token mới

### Lỗi 3: "401 Unauthorized" - Token không hợp lệ

**Nguyên nhân:** Token bị sai format hoặc signature không đúng
**Giải pháp:** Kiểm tra JWT Secret trong appsettings.json có khớp không

### Lỗi 4: "403 Forbidden" - Không đủ quyền

**Nguyên nhân:** Role không phải Admin hoặc Teacher
**Giải pháp:** Đăng nhập với tài khoản có role Admin/Teacher

## 💡 Tip: Test với Swagger UI

1. Mở Swagger UI: `https://your-api/swagger`
2. Click nút **"Authorize"** ở trên cùng
3. Nhập: `Bearer YOUR_TOKEN` (bao gồm cả chữ "Bearer ")
4. Click **"Authorize"**
5. Test API `/api/GDPT/import-from-file`

## 🔄 Refresh Token (Nếu cần)

Nếu token hết hạn, đăng nhập lại:

```bash
POST /api/Auth/login
Content-Type: application/json

{
  "username": "VietLuan",
  "password": "your_password"
}
```

Lấy token mới từ response và dùng để test lại.
