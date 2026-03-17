# Online Exam System — Backend API

> **ASP.NET Core Web API (.NET 10)** | Clean Architecture | Entity Framework Core | SQL Server

**Base URL:** `http://localhost:5082/api/v1`

---

## Công nghệ sử dụng

| Công nghệ | Phiên bản |
|-----------|-----------|
| .NET | 10.0 |
| ASP.NET Core Web API | 10.0 |
| Entity Framework Core | 10.0.1 |
| SQL Server | Latest |
| AutoMapper | 12.0.1 |
| FluentValidation | 11.12.0 |
| Password Hashing | ASP.NET Core Identity PasswordHasher |

---

## Cài đặt & Chạy

```bash
cd ExamSystem
dotnet restore
dotnet ef database update
dotnet run
```

Ứng dụng chạy tại: `http://localhost:5082`
Swagger UI: `http://localhost:5082/swagger`

### Cấu hình Admin mặc định (`appsettings.json`)

```json
"AdminSettings": {
  "Username": "admin",
  "Email": "admin@example.com",
  "FullName": "Super Admin",
  "Password": "Admin@123"
}
```

Khi khởi động lần đầu, hệ thống tự động seed:
- 3 roles mặc định: `Admin`, `Teacher`, `Student`
- 34 permissions theo nhóm chức năng
- 1 tài khoản admin với full quyền (từ `AdminSettings`)

---

## Kiến trúc hệ thống

```
Controllers/V1/          — Nhận request, trả response
Services/                — Business logic
Repositories/            — Truy vấn database
DTOs/                    — Data Transfer Objects
Models/                  — Entity models (EF Core)
Authorization/           — Permission-based auth
Middleware/              — Error handling, auth
Data/                    — DbContext, DbSeeder
Validators/              — FluentValidation rules
Mappings/                — AutoMapper profiles
```

---

## Xác thực & Phân quyền

### JWT Access Token
- **Lifetime:** 15 phút
- **Algorithm:** HMAC-SHA256
- **Gửi qua:** `Authorization: Bearer {token}`

### Refresh Token
- **Lifetime:** 7 ngày
- **Lưu trữ:** HttpOnly Cookie (`refresh_token`)
- **Cơ chế:** Rotation — mỗi lần refresh, token cũ bị revoke, trả token mới

### Permission-based Authorization
Mỗi endpoint được bảo vệ bằng `[RequirePermission("CODE")]`.
Hệ thống kiểm tra: `User → UserRoles → Role → RolePermissions → Permission.Code`

---

## Tổng quan APIs đã hoàn thành

**Tổng: 25 APIs**

| Nhóm | Số API | Trạng thái |
|------|--------|-----------|
| Authentication | 4 | ✅ |
| User Management | 9 | ✅ |
| Role & Permission | 12 | ✅ |

---

# NHÓM 1: AUTHENTICATION (4 APIs)

## 1.1 Đăng ký

**`POST /api/v1/auth/register`** — Không yêu cầu xác thực

**Request Body:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123",
  "fullName": "John Doe"
}
```

**Response 201:**
```json
{
  "statusCode": 201,
  "error": null,
  "message": "Đăng ký thành công",
  "data": {
    "access_token": "eyJhbGci...",
    "expires_in": 900,
    "user": {
      "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
      "username": "john_doe",
      "email": "john@example.com",
      "fullName": "John Doe",
      "isActive": true,
      "createdAt": "2026-03-17T10:00:00Z"
    }
  }
}
```

**Set-Cookie:** `refresh_token=...; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth`

> Sau khi đăng ký, tài khoản được tự động gán role `Student`.

---

## 1.2 Đăng nhập

**`POST /api/v1/auth/login`** — Không yêu cầu xác thực

**Request Body:**
```json
{
  "usernameOrEmail": "john_doe",
  "password": "SecurePass123"
}
```

> `usernameOrEmail` chấp nhận cả username lẫn email.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đăng nhập thành công",
  "data": {
    "access_token": "eyJhbGci...",
    "expires_in": 900,
    "user": { ... }
  }
}
```

**Response 401 — Sai mật khẩu:**
```json
{
  "statusCode": 401,
  "error": { "code": "UNAUTHORIZED", "reason": "Tên đăng nhập hoặc mật khẩu không đúng" },
  "message": "Tên đăng nhập hoặc mật khẩu không đúng",
  "data": null
}
```

**Response 401 — Tài khoản bị khóa:**
```json
{
  "statusCode": 401,
  "error": { "code": "UNAUTHORIZED", "reason": "Tài khoản đã bị khóa" },
  "message": "Tài khoản đã bị khóa",
  "data": null
}
```

---

## 1.3 Làm mới token

**`POST /api/v1/auth/refresh`** — Không yêu cầu xác thực (dùng cookie)

**Request:** Không có body. Cookie `refresh_token` được gửi tự động.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Làm mới token thành công",
  "data": {
    "access_token": "eyJhbGci...",
    "expires_in": 900,
    "user": { ... }
  }
}
```

**Set-Cookie:** Refresh token mới được set. Token cũ bị revoke.

---

## 1.4 Đăng xuất

**`POST /api/v1/auth/logout`** — Yêu cầu xác thực

**Request:** Không có body.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đăng xuất thành công",
  "data": null
}
```

**Set-Cookie:** Cookie `refresh_token` bị xóa. Token bị revoke trong DB.

---

# NHÓM 2: USER MANAGEMENT (9 APIs)

> Tất cả endpoints yêu cầu `Authorization: Bearer {token}` và permission tương ứng.

## 2.1 Lấy danh sách users

**`GET /api/v1/users`** — Permission: `USER_VIEW`
**Query Params:** `page` (optional), `pageSize` (optional, default 20)

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy danh sách người dùng thành công",
  "data": {
    "meta": { "page": 1, "pageSize": 20, "pages": 1, "total": 5 },
    "result": [
      {
        "id": "...",
        "username": "john_doe",
        "email": "john@example.com",
        "fullName": "John Doe",
        "isActive": true,
        "createdAt": "2026-03-17T10:00:00Z",
        "roles": ["Student"]
      }
    ]
  }
}
```

---

## 2.2 Lấy user theo ID

**`GET /api/v1/users/{id}`** — Permission: `USER_VIEW`

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy thông tin người dùng thành công",
  "data": { "id": "...", "username": "...", "email": "...", "fullName": "...", "isActive": true, "createdAt": "..." }
}
```

---

## 2.3 Lấy user kèm roles

**`GET /api/v1/users/{id}/roles`** — Permission: `USER_VIEW`

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy người dùng kèm vai trò thành công",
  "data": {
    "id": "...",
    "username": "john_doe",
    "email": "john@example.com",
    "fullName": "John Doe",
    "isActive": true,
    "createdAt": "...",
    "roles": [
      { "id": "...", "name": "Student", "description": "...", "createdAt": "..." }
    ]
  }
}
```

---

## 2.4 Tạo user

**`POST /api/v1/users`** — Permission: `USER_CREATE`

**Request Body:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123",
  "fullName": "John Doe"
}
```

**Response 201:** Trả về `UserDto` của user vừa tạo.

---

## 2.5 Cập nhật user

**`PUT /api/v1/users/{id}`** — Permission: `USER_UPDATE`

**Request Body** (tất cả optional):
```json
{
  "username": "new_username",
  "email": "new@example.com",
  "fullName": "New Name",
  "isActive": false
}
```

**Response 200:** Trả về `UserDto` đã cập nhật.

---

## 2.6 Xóa user

**`DELETE /api/v1/users/{id}`** — Permission: `USER_DELETE`

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Xóa người dùng thành công",
  "data": null
}
```

---

## 2.7 Gán roles cho user

**`POST /api/v1/users/{id}/roles`** — Permission: `USER_UPDATE`

**Request Body:**
```json
{
  "roleIds": ["guid-role-1", "guid-role-2"]
}
```

> **Lưu ý:** THAY THẾ toàn bộ roles cũ bằng danh sách mới.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Gán vai trò cho người dùng thành công",
  "data": null
}
```

---

## 2.8 Đổi mật khẩu

**`POST /api/v1/users/{id}/change-password`** — Permission: `USER_UPDATE`

**Request Body:**
```json
{
  "currentPassword": "OldPass123",
  "newPassword": "NewPass456"
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

## 2.9 Khóa / Mở khóa tài khoản

**`PATCH /api/v1/users/{id}/toggle-lock`** — Permission: `USER_UPDATE`

**Request:** Không có body.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đã khóa tài khoản thành công",
  "data": { "id": "...", "username": "...", "isActive": false, ... }
}
```

> Message trả về "Đã khóa" hoặc "Đã mở khóa" tùy trạng thái sau khi toggle.

---

# NHÓM 3: ROLE & PERMISSION (12 APIs)

> Tất cả endpoints yêu cầu `Authorization: Bearer {token}` và permission tương ứng.

## 3.1 Lấy danh sách roles

**`GET /api/v1/roles`** — Permission: `ROLE_VIEW`
**Query Params:** `page`, `pageSize` (optional)

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy danh sách vai trò thành công",
  "data": {
    "meta": { "page": 1, "pageSize": 3, "pages": 1, "total": 3 },
    "result": [
      { "id": "...", "name": "Admin", "description": "...", "createdAt": "..." }
    ]
  }
}
```

---

## 3.2 Lấy role theo ID

**`GET /api/v1/roles/{id}`** — Permission: `ROLE_VIEW`

**Response 200:** Trả về `RoleDto`.

---

## 3.3 Lấy role kèm permissions

**`GET /api/v1/roles/{id}/permissions`** — Permission: `ROLE_VIEW`

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy vai trò kèm quyền thành công",
  "data": {
    "id": "...",
    "name": "Teacher",
    "description": "...",
    "createdAt": "...",
    "permissions": [
      { "id": "...", "code": "EXAM_VIEW", "description": "Xem danh sách bài thi" }
    ]
  }
}
```

---

## 3.4 Tạo role

**`POST /api/v1/roles`** — Permission: `ROLE_CREATE`

**Request Body:**
```json
{
  "name": "Moderator",
  "description": "Vai trò kiểm duyệt"
}
```

**Response 201:** Trả về `RoleDto` vừa tạo.

---

## 3.5 Cập nhật role

**`PUT /api/v1/roles/{id}`** — Permission: `ROLE_UPDATE`

**Request Body:**
```json
{
  "name": "Senior Teacher",
  "description": "Mô tả mới"
}
```

**Response 200:** Trả về `RoleDto` đã cập nhật.

---

## 3.6 Xóa role

**`DELETE /api/v1/roles/{id}`** — Permission: `ROLE_DELETE`

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Xóa vai trò thành công",
  "data": null
}
```

---

## 3.7 Gán permissions cho role

**`POST /api/v1/roles/{id}/permissions`** — Permission: `ROLE_ASSIGN_PERMISSION`

**Request Body:**
```json
{
  "permissionIds": ["guid-perm-1", "guid-perm-2"]
}
```

> **Lưu ý:** THAY THẾ toàn bộ permissions cũ bằng danh sách mới.

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Cập nhật quyền cho vai trò thành công",
  "data": null
}
```

---

## 3.8 Lấy danh sách permissions

**`GET /api/v1/permissions`** — Permission: `PERMISSION_VIEW`
**Query Params:** `page`, `pageSize` (optional)

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy danh sách quyền thành công",
  "data": {
    "meta": { "page": 1, "pageSize": 34, "pages": 1, "total": 34 },
    "result": [
      { "id": "...", "code": "USER_VIEW", "description": "Xem danh sách người dùng" }
    ]
  }
}
```

---

## 3.9 Lấy permission theo ID

**`GET /api/v1/permissions/{id}`** — Permission: `PERMISSION_VIEW`

**Response 200:** Trả về `PermissionDto`.

---

## 3.10 Tạo permission

**`POST /api/v1/permissions`** — Permission: `PERMISSION_CREATE`

**Request Body:**
```json
{
  "code": "REPORT_VIEW",
  "description": "Xem báo cáo"
}
```

> `code` phải theo định dạng `UPPER_SNAKE_CASE`.

**Response 201:** Trả về `PermissionDto` vừa tạo.

---

## 3.11 Cập nhật permission

**`PUT /api/v1/permissions/{id}`** — Permission: `PERMISSION_UPDATE`

**Request Body:**
```json
{
  "code": "REPORT_MANAGE",
  "description": "Quản lý báo cáo"
}
```

**Response 200:** Trả về `PermissionDto` đã cập nhật.

---

## 3.12 Xóa permission

**`DELETE /api/v1/permissions/{id}`** — Permission: `PERMISSION_DELETE`

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Xóa quyền thành công",
  "data": null
}
```

---

# Danh sách Permissions mặc định (seed)

| Nhóm | Permission Code |
|------|----------------|
| User | `USER_VIEW`, `USER_CREATE`, `USER_UPDATE`, `USER_DELETE` |
| Role | `ROLE_VIEW`, `ROLE_CREATE`, `ROLE_UPDATE`, `ROLE_DELETE`, `ROLE_ASSIGN_PERMISSION` |
| Permission | `PERMISSION_VIEW`, `PERMISSION_CREATE`, `PERMISSION_UPDATE`, `PERMISSION_DELETE` |
| Exam | `EXAM_VIEW`, `EXAM_CREATE`, `EXAM_UPDATE`, `EXAM_DELETE`, `EXAM_PUBLISH`, `EXAM_ASSIGN` |
| Question | `QUESTION_VIEW`, `QUESTION_CREATE`, `QUESTION_UPDATE`, `QUESTION_DELETE` |
| Group | `GROUP_VIEW`, `GROUP_CREATE`, `GROUP_UPDATE`, `GROUP_DELETE`, `GROUP_MANAGE_MEMBER` |
| Subject | `SUBJECT_VIEW`, `SUBJECT_CREATE`, `SUBJECT_UPDATE`, `SUBJECT_DELETE` |
| Result | `RESULT_VIEW`, `RESULT_VIEW_ALL` |

---

# Xử lý lỗi chung

**401 — Chưa đăng nhập / Token hết hạn:**
```json
{
  "statusCode": 401,
  "error": { "code": "UNAUTHORIZED", "reason": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn" },
  "message": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn",
  "data": null
}
```

**403 — Không có quyền:**
```json
{
  "statusCode": 403,
  "error": { "code": "FORBIDDEN", "reason": "Bạn không có quyền thực hiện thao tác này" },
  "message": "Bạn không có quyền thực hiện thao tác này",
  "data": null
}
```

**400 — Validation:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [{ "field": "Username", "message": "Username is required" }]
  },
  "message": "Dữ liệu không hợp lệ",
  "data": null
}
```

**404 — Không tìm thấy:**
```json
{
  "statusCode": 404,
  "error": { "code": "NOT_FOUND", "resource": "Người dùng với id '...'" },
  "message": "Không tìm thấy người dùng",
  "data": null
}
```

---

**Cập nhật:** 17/03/2026 | **Phiên bản:** v4.0 | **APIs:** 25/25 ✅
