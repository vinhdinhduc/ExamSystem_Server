# ExamSystem API — Tài liệu API V2

**Base URL:** `http://localhost:5082/api/v1`
**Auth:** Bearer Token (JWT, 15 phút) + HttpOnly Cookie (refresh_token, 7 ngày)

---

## Cấu trúc Response chung

### Thành công
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Mô tả kết quả",
  "data": { ... }
}
```

### Thành công có phân trang
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Mô tả kết quả",
  "data": {
    "meta": {
      "page": 1,
      "pageSize": 20,
      "pages": 5,
      "total": 100
    },
    "result": [ ... ]
  }
}
```

### Lỗi validation (400)
```json
{
  "statusCode": 400,
  "error": {
    "details": [
      { "field": "Email", "message": "Email không hợp lệ" }
    ]
  },
  "message": "Dữ liệu không hợp lệ",
  "data": null
}
```

### Lỗi không tìm thấy (404)
```json
{
  "statusCode": 404,
  "error": { "resource": "Người dùng với id '...'" },
  "message": "Không tìm thấy người dùng",
  "data": null
}
```

### Lỗi unauthorized (401)
```json
{
  "statusCode": 401,
  "error": { "reason": "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại" },
  "message": "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại",
  "data": null
}
```

---

## 1. Auth — `/auth`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/auth/register` | ❌ | Đăng ký tài khoản |
| POST | `/auth/login` | ❌ | Đăng nhập |
| POST | `/auth/refresh` | ❌ (cookie) | Làm mới access token |
| POST | `/auth/logout` | ❌ (cookie) | Đăng xuất |
| GET | `/auth/verify-email?token=...` | ❌ | Xác thực email |
| POST | `/auth/forgot-password` | ❌ | Gửi OTP quên mật khẩu |
| POST | `/auth/reset-password` | ❌ | Đặt lại mật khẩu bằng OTP |

### POST `/auth/register`
**Request:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "username": "nguyen_van_a",
  "fullName": "Nguyễn Văn A"
}
```
> `username` và `fullName` là tuỳ chọn. Nếu không truyền, hệ thống tự sinh từ email.

**Response 201:**
```json
{
  "statusCode": 201,
  "error": null,
  "message": "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.",
  "data": { "email": "user@example.com" }
}
```

---

### POST `/auth/login`
**Request:**
```json
{
  "usernameOrEmail": "user@example.com",
  "password": "password123"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đăng nhập thành công",
  "data": {
    "access_token": "eyJhbGciOiJIUzI1NiIs...",
    "expires_in": 900,
    "user": {
      "id": "f1be154d-c621-f111-ad14-00090ffe0001",
      "username": "nguyen_van_a",
      "fullName": "Nguyễn Văn A",
      "email": "user@example.com",
      "isActive": true,
      "createdAt": "2024-01-15T08:00:00Z",
      "roles": ["student"]
    }
  }
}
```
> Refresh token được set tự động vào HttpOnly Cookie `refresh_token`.

**Response 401 — Email chưa xác thực:**
```json
{
  "statusCode": 401,
  "error": { "code": "EMAIL_NOT_VERIFIED" },
  "message": "Email chưa được xác thực. Vui lòng kiểm tra hộp thư và click vào link xác thực.",
  "data": null
}
```

---

### POST `/auth/refresh`
> Không cần body. Tự động đọc cookie `refresh_token`.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Làm mới token thành công",
  "data": {
    "access_token": "eyJhbGciOiJIUzI1NiIs...",
    "expires_in": 900,
    "user": { ... }
  }
}
```

---

### POST `/auth/forgot-password`
**Request:**
```json
{ "email": "user@example.com" }
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Nếu email tồn tại, mã OTP đã được gửi. Vui lòng kiểm tra hộp thư.",
  "data": null
}
```

---

### POST `/auth/reset-password`
**Request:**
```json
{
  "email": "user@example.com",
  "otp": "123456",
  "newPassword": "newpassword123",
  "confirmPassword": "newpassword123"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.",
  "data": null
}
```

---

## 2. Users — `/users`

### Self-service (chỉ cần `[Authorize]` — không cần permission)

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/users/me` | ✅ Bearer | Lấy thông tin cá nhân |
| PUT | `/users/me` | ✅ Bearer | Cập nhật thông tin cá nhân |
| POST | `/users/me/change-password` | ✅ Bearer | Đổi mật khẩu |

### GET `/users/me`
**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy thông tin cá nhân thành công",
  "data": {
    "id": "f1be154d-c621-f111-ad14-00090ffe0001",
    "username": "nguyen_van_a",
    "email": "user@example.com",
    "fullName": "Nguyễn Văn A",
    "isActive": true,
    "createdAt": "2024-01-15T08:00:00Z"
  }
}
```

### PUT `/users/me`
**Request:**
```json
{
  "fullName": "Nguyễn Văn B",
  "username": "nguyen_van_b"
}
```
> Tất cả các field là tuỳ chọn (`null` = không cập nhật). Không được truyền empty string.

**Response 200:** Trả về `UserDto` sau khi cập nhật (cấu trúc giống `GET /users/me`).

---

### POST `/users/me/change-password`
**Request:**
```json
{
  "currentPassword": "oldpassword123",
  "newPassword": "newpassword456"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đổi mật khẩu thành công",
  "data": null
}
```

---

### Admin endpoints (cần permission tương ứng)

| Method | Endpoint | Permission | Mô tả |
|--------|----------|-----------|-------|
| GET | `/users` | `USER_VIEW` | Danh sách users (phân trang, kèm roles) |
| GET | `/users/{id}` | `USER_VIEW` | Lấy user theo ID |
| GET | `/users/{id}/roles` | `USER_VIEW` | Lấy user kèm chi tiết roles |
| POST | `/users` | `USER_CREATE` | Tạo user mới |
| PUT | `/users/{id}` | `USER_UPDATE` | Cập nhật user |
| DELETE | `/users/{id}` | `USER_DELETE` | Xóa user |
| POST | `/users/{id}/roles` | `USER_UPDATE` | Gán roles cho user |
| PATCH | `/users/{id}/toggle-lock` | `USER_UPDATE` | Khoá/mở khoá tài khoản |
| POST | `/users/{id}/change-password` | `USER_UPDATE` | Admin đổi mật khẩu user |

### GET `/users?page=1&pageSize=20`
**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy danh sách người dùng thành công",
  "data": {
    "meta": { "page": 1, "pageSize": 20, "pages": 3, "total": 55 },
    "result": [
      {
        "id": "f1be154d-c621-f111-ad14-00090ffe0001",
        "username": "nguyen_van_a",
        "email": "user@example.com",
        "fullName": "Nguyễn Văn A",
        "isActive": true,
        "createdAt": "2024-01-15T08:00:00Z",
        "roles": ["student", "teacher"]
      }
    ]
  }
}
```

### POST `/users`
**Request:**
```json
{
  "username": "nguyen_van_a",
  "email": "user@example.com",
  "password": "password123",
  "fullName": "Nguyễn Văn A"
}
```

### GET `/users/{id}/roles`
**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy người dùng kèm vai trò thành công",
  "data": {
    "id": "f1be154d-c621-f111-ad14-00090ffe0001",
    "username": "nguyen_van_a",
    "email": "user@example.com",
    "fullName": "Nguyễn Văn A",
    "isActive": true,
    "createdAt": "2024-01-15T08:00:00Z",
    "roles": [
      {
        "id": "a1b2c3d4-...",
        "name": "student",
        "description": "Học sinh",
        "createdAt": "2024-01-01T00:00:00Z"
      }
    ]
  }
}
```

### POST `/users/{id}/roles`
**Request:**
```json
{
  "roleIds": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

### PATCH `/users/{id}/toggle-lock`
> Không cần body. Toggle `isActive` giữa `true`/`false`.

**Response 200:** Trả về `UserDto` với `isActive` mới.

---

## 3. Roles — `/roles`

| Method | Endpoint | Permission | Mô tả |
|--------|----------|-----------|-------|
| GET | `/roles` | `ROLE_VIEW` | Danh sách roles (phân trang) |
| GET | `/roles/{id}` | `ROLE_VIEW` | Lấy role theo ID |
| GET | `/roles/{id}/permissions` | `ROLE_VIEW` | Lấy role kèm danh sách permissions |
| POST | `/roles` | `ROLE_CREATE` | Tạo role mới |
| PUT | `/roles/{id}` | `ROLE_UPDATE` | Cập nhật role |
| DELETE | `/roles/{id}` | `ROLE_DELETE` | Xóa role |
| POST | `/roles/{id}/permissions` | `ROLE_ASSIGN_PERMISSION` | Gán permissions cho role |

### GET `/roles/{id}/permissions`
**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy vai trò kèm quyền thành công",
  "data": {
    "id": "a1b2c3d4-...",
    "name": "teacher",
    "description": "Giáo viên",
    "createdAt": "2024-01-01T00:00:00Z",
    "permissions": [
      { "id": "p1p2p3p4-...", "code": "EXAM_CREATE", "description": "Tạo bài thi" },
      { "id": "p2p3p4p5-...", "code": "QUESTION_CREATE", "description": "Tạo câu hỏi" }
    ]
  }
}
```

### POST `/roles`
**Request:**
```json
{
  "name": "teacher",
  "description": "Giáo viên"
}
```

### POST `/roles/{id}/permissions`
> Gán toàn bộ danh sách (replace, không phải append).

**Request:**
```json
{
  "permissionIds": [
    "p1p2p3p4-e5f6-7890-abcd-ef1234567890",
    "p2p3p4p5-f6a7-8901-bcde-f12345678901"
  ]
}
```

---

## 4. Permissions — `/permissions`

| Method | Endpoint | Permission | Mô tả |
|--------|----------|-----------|-------|
| GET | `/permissions` | `PERMISSION_VIEW` | Danh sách permissions (phân trang) |
| GET | `/permissions/{id}` | `PERMISSION_VIEW` | Lấy permission theo ID |
| POST | `/permissions` | `PERMISSION_CREATE` | Tạo permission mới |
| PUT | `/permissions/{id}` | `PERMISSION_UPDATE` | Cập nhật permission |
| DELETE | `/permissions/{id}` | `PERMISSION_DELETE` | Xóa permission |

### POST `/permissions`
**Request:**
```json
{
  "code": "EXAM_CREATE",
  "description": "Tạo bài thi mới"
}
```

**Response 201:**
```json
{
  "statusCode": 201,
  "error": null,
  "message": "Tạo quyền thành công",
  "data": {
    "id": "p1p2p3p4-e5f6-7890-abcd-ef1234567890",
    "code": "EXAM_CREATE",
    "description": "Tạo bài thi mới"
  }
}
```

### PUT `/permissions/{id}`
**Request:**
```json
{
  "code": "EXAM_CREATE",
  "description": "Cập nhật mô tả quyền tạo bài thi"
}
```

---

## 5. Danh sách Permission Codes

| Code | Mô tả |
|------|-------|
| `USER_VIEW` | Xem danh sách và thông tin người dùng |
| `USER_CREATE` | Tạo người dùng mới |
| `USER_UPDATE` | Cập nhật, khoá/mở khoá, gán roles |
| `USER_DELETE` | Xoá người dùng |
| `ROLE_VIEW` | Xem danh sách roles |
| `ROLE_CREATE` | Tạo role mới |
| `ROLE_UPDATE` | Cập nhật role |
| `ROLE_DELETE` | Xoá role |
| `ROLE_ASSIGN_PERMISSION` | Gán permissions cho role |
| `PERMISSION_VIEW` | Xem danh sách permissions |
| `PERMISSION_CREATE` | Tạo permission mới |
| `PERMISSION_UPDATE` | Cập nhật permission |
| `PERMISSION_DELETE` | Xoá permission |
| `SUBJECT_VIEW` | Xem môn học |
| `SUBJECT_CREATE` | Tạo môn học |
| `SUBJECT_UPDATE` | Cập nhật môn học |
| `SUBJECT_DELETE` | Xoá môn học |
| `QUESTION_VIEW` | Xem câu hỏi |
| `QUESTION_CREATE` | Tạo câu hỏi |
| `QUESTION_UPDATE` | Cập nhật câu hỏi |
| `QUESTION_DELETE` | Xoá câu hỏi |
| `EXAM_VIEW` | Xem bài thi |
| `EXAM_CREATE` | Tạo bài thi |
| `EXAM_UPDATE` | Cập nhật bài thi |
| `EXAM_DELETE` | Xoá bài thi |
| `EXAM_ASSIGN` | Giao bài thi cho học sinh/nhóm |
| `EXAM_PUBLISH` | Xuất bản bài thi |
| `GROUP_VIEW` | Xem nhóm |
| `GROUP_CREATE` | Tạo nhóm |
| `GROUP_UPDATE` | Cập nhật nhóm |
| `GROUP_DELETE` | Xoá nhóm |
| `GROUP_MEMBER_MANAGE` | Quản lý thành viên nhóm |
| `EXAM_SESSION_VIEW` | Xem phiên thi |
| `EXAM_SESSION_GRADE` | Chấm điểm phiên thi |

---

## Ghi chú

- **Versioning:** Tất cả endpoint có prefix `/api/v1/`
- **Refresh token:** HttpOnly cookie, chỉ gửi đến `/api/v1/auth`
- **Phân trang:** Query params `?page=1&pageSize=20` (pageSize mặc định 20)
- **Partial update:** Các PUT endpoint hỗ trợ partial — chỉ gửi field cần cập nhật (dùng `null`/bỏ qua field không muốn đổi)
- **Subjects, Questions, Exams, Groups, ExamSessions:** Chưa có controller (Dev 2 implement)
