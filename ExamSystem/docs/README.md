# 🎓 Online Exam System - Backend API Documentation

> **ASP.NET Core Web API (.NET 10)** | Clean Architecture | Entity Framework Core

**Base URL:** `http://localhost:5082/api/v1`

---

## 📌 Tổng quan

Hệ thống quản lý thi trực tuyến với kiến trúc Clean Architecture.

**Tổng APIs đã hoàn thành: 24 APIs**
- ✅ **Nhóm 1: Role & Permission** (12 APIs)
- ✅ **Nhóm 2: User Management** (8 APIs)
- ✅ **Nhóm 3: Authentication** (4 APIs)

---

## 🚀 Công nghệ

| Công nghệ | Phiên bản |
|-----------|-----------|
| .NET | 10.0 |
| ASP.NET Core Web API | 10.0 |
| Entity Framework Core | 10.0.1 |
| SQL Server | Latest |
| AutoMapper | 12.0.1 |
| FluentValidation | 11.12.0 |
| Password Hashing | ASP.NET Core Identity |

---

## 📊 Database Schema

### Roles
```
Id (Guid PK) - NEWSEQUENTIALID()
Name (nvarchar(100), unique)
Description (nvarchar(500), nullable)
CreatedAt (datetime2)
```

### Permissions
```
Id (Guid PK) - NEWSEQUENTIALID()
Code (nvarchar(100), unique)
Description (nvarchar(500), nullable)
```

### RolePermissions
```
RoleId (Guid PK, FK → Roles)
PermissionId (Guid PK, FK → Permissions)
```

### Users
```
Id (Guid PK) - NEWSEQUENTIALID()
Username (nvarchar(50), unique)
Email (nvarchar(100), unique)
PasswordHash (nvarchar(500))
FullName (nvarchar(100))
IsActive (bit, default 1)
CreatedAt (datetime2)
```

### UserRoles
```
UserId (Guid PK, FK → Users)
RoleId (Guid PK, FK → Roles)
```

### RefreshTokens
```
Id (int PK, identity)
UserId (Guid, FK → Users)
Token (nvarchar(500))
ExpiresAt (datetime2)
IsRevoked (bit, default 0)
CreatedAt (datetime2)
```

---

# 📡 NHÓM 1: ROLE & PERMISSION (12 APIs)

## 1.1 GET All Roles

**Method:** `GET`  
**Endpoint:** `/api/v1/roles`  
**Query Params:** `page` (optional), `pageSize` (optional)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Roles retrieved successfully",
  "data": {
    "meta": {
      "page": 1,
      "pageSize": 3,
      "pages": 1,
      "total": 3
    },
    "result": [
      {
        "id": "2164fb32-ab1e-f111-ad11-00090ffe0001",
        "name": "Admin",
        "description": "Administrator role",
        "createdAt": "2024-01-20T10:00:00Z"
      }
    ]
  }
}
```

---

## 1.2 GET Role by ID

**Method:** `GET`  
**Endpoint:** `/api/v1/roles/{id}`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Role retrieved successfully",
  "data": {
    "id": "2164fb32-ab1e-f111-ad11-00090ffe0001",
    "name": "Admin",
    "description": "Administrator role",
    "createdAt": "2024-01-20T10:00:00Z"
  }
}
```

**Response 404:**
```json
{
  "statusCode": 404,
  "error": {
    "code": "NOT_FOUND",
    "resource": "Role with id '...'"
  },
  "message": "Role not found",
  "data": null
}
```

---

## 1.3 GET Role with Permissions

**Method:** `GET`  
**Endpoint:** `/api/v1/roles/{id}/permissions`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Role with permissions retrieved successfully",
  "data": {
    "id": "2164fb32-ab1e-f111-ad11-00090ffe0001",
    "name": "Admin",
    "description": "Administrator role",
    "createdAt": "2024-01-20T10:00:00Z",
    "permissions": [
      {
        "id": "1164fb32-ab1e-f111-ad11-00090ffe0001",
        "code": "USER_VIEW",
        "description": "Xem danh sách người dùng"
      }
    ]
  }
}
```

---

## 1.4 CREATE Role

**Method:** `POST`  
**Endpoint:** `/api/v1/roles`

**Request Body:**
```json
{
  "name": "Manager",
  "description": "Manager role"
}
```

**Response 201:**
```json
{
  "statusCode": 201,
  "error": null,
  "message": "Role created successfully",
  "data": {
    "id": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "name": "Manager",
    "description": "Manager role",
    "createdAt": "2024-01-20T11:00:00Z"
  }
}
```

**Response 400 - Validation:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [
      {
        "field": "Name",
        "message": "Name is required"
      }
    ]
  },
  "message": "Validation failed",
  "data": null
}
```

**Response 400 - Duplicate:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "BUSINESS_ERROR",
    "reason": "Role with name 'Admin' already exists"
  },
  "message": "Role with name 'Admin' already exists",
  "data": null
}
```

---

## 1.5 UPDATE Role

**Method:** `PUT`  
**Endpoint:** `/api/v1/roles/{id}`  
**Path Params:** `id` (Guid, required)

**Request Body:**
```json
{
  "name": "Senior Manager",
  "description": "Updated description"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Role updated successfully",
  "data": {
    "id": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "name": "Senior Manager",
    "description": "Updated description",
    "createdAt": "2024-01-20T11:00:00Z"
  }
}
```

---

## 1.6 DELETE Role

**Method:** `DELETE`  
**Endpoint:** `/api/v1/roles/{id}`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Role deleted successfully",
  "data": null
}
```

---

## 1.7 ASSIGN Permissions to Role

**Method:** `POST`  
**Endpoint:** `/api/v1/roles/{id}/permissions`  
**Path Params:** `id` (Guid, required)

**Request Body:**
```json
{
  "permissionIds": [
    "1164fb32-ab1e-f111-ad11-00090ffe0001",
    "2464fb32-ab1e-f111-ad11-00090ffe0001"
  ]
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Permissions assigned to role successfully",
  "data": null
}
```

---

## 1.8 GET All Permissions

**Method:** `GET`  
**Endpoint:** `/api/v1/permissions`  
**Query Params:** `page` (optional), `pageSize` (optional)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Permissions retrieved successfully",
  "data": {
    "meta": {
      "page": 1,
      "pageSize": 33,
      "pages": 1,
      "total": 33
    },
    "result": [
      {
        "id": "1164fb32-ab1e-f111-ad11-00090ffe0001",
        "code": "USER_VIEW",
        "description": "Xem danh sách người dùng"
      }
    ]
  }
}
```

---

## 1.9 GET Permission by ID

**Method:** `GET`  
**Endpoint:** `/api/v1/permissions/{id}`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Permission retrieved successfully",
  "data": {
    "id": "1164fb32-ab1e-f111-ad11-00090ffe0001",
    "code": "USER_VIEW",
    "description": "Xem danh sách người dùng"
  }
}
```

---

## 1.10 CREATE Permission

**Method:** `POST`  
**Endpoint:** `/api/v1/permissions`

**Request Body:**
```json
{
  "code": "REPORT_VIEW",
  "description": "Xem báo cáo"
}
```

**Response 201:**
```json
{
  "statusCode": 201,
  "error": null,
  "message": "Permission created successfully",
  "data": {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afaa",
    "code": "REPORT_VIEW",
    "description": "Xem báo cáo"
  }
}
```

**Response 400 - Validation:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [
      {
        "field": "Code",
        "message": "Code must be in UPPERCASE_WITH_UNDERSCORES format"
      }
    ]
  },
  "message": "Validation failed",
  "data": null
}
```

---

## 1.11 UPDATE Permission

**Method:** `PUT`  
**Endpoint:** `/api/v1/permissions/{id}`  
**Path Params:** `id` (Guid, required)

**Request Body:**
```json
{
  "code": "REPORT_MANAGE",
  "description": "Quản lý báo cáo"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Permission updated successfully",
  "data": {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afaa",
    "code": "REPORT_MANAGE",
    "description": "Quản lý báo cáo"
  }
}
```

---

## 1.12 DELETE Permission

**Method:** `DELETE`  
**Endpoint:** `/api/v1/permissions/{id}`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Permission deleted successfully",
  "data": null
}
```

---

# 📡 NHÓM 2: USER MANAGEMENT (8 APIs)

## 2.1 GET All Users

**Method:** `GET`  
**Endpoint:** `/api/v1/users`  
**Query Params:** `page` (optional), `pageSize` (optional)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Users retrieved successfully",
  "data": {
    "meta": {
      "page": 1,
      "pageSize": 10,
      "pages": 1,
      "total": 5
    },
    "result": [
      {
        "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
        "username": "john_doe",
        "email": "john@example.com",
        "fullName": "John Doe",
        "isActive": true,
        "createdAt": "2024-01-20T12:00:00Z"
      }
    ]
  }
}
```

**⚠️ Lưu ý:** Response KHÔNG chứa `passwordHash`

---

## 2.2 GET User by ID

**Method:** `GET`  
**Endpoint:** `/api/v1/users/{id}`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "User retrieved successfully",
  "data": {
    "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
    "username": "john_doe",
    "email": "john@example.com",
    "fullName": "John Doe",
    "isActive": true,
    "createdAt": "2024-01-20T12:00:00Z"
  }
}
```

---

## 2.3 GET User with Roles

**Method:** `GET`  
**Endpoint:** `/api/v1/users/{id}/roles`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "User with roles retrieved successfully",
  "data": {
    "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
    "username": "john_doe",
    "email": "john@example.com",
    "fullName": "John Doe",
    "isActive": true,
    "createdAt": "2024-01-20T12:00:00Z",
    "roles": [
      {
        "id": "2264fb32-ab1e-f111-ad11-00090ffe0001",
        "name": "Teacher",
        "description": "Teacher role",
        "createdAt": "2024-01-20T10:00:00Z"
      }
    ]
  }
}
```

---

## 2.4 CREATE User

**Method:** `POST`  
**Endpoint:** `/api/v1/users`

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
  "message": "User created successfully",
  "data": {
    "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
    "username": "john_doe",
    "email": "john@example.com",
    "fullName": "John Doe",
    "isActive": true,
    "createdAt": "2024-01-20T12:00:00Z"
  }
}
```

**Response 400 - Duplicate Username:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "BUSINESS_ERROR",
    "reason": "Username 'john_doe' is already taken"
  },
  "message": "Username 'john_doe' is already taken",
  "data": null
}
```

---

## 2.5 UPDATE User

**Method:** `PUT`  
**Endpoint:** `/api/v1/users/{id}`  
**Path Params:** `id` (Guid, required)

**Request Body:**
```json
{
  "username": "john_doe_updated",
  "email": "john_new@example.com",
  "fullName": "John Doe Updated",
  "isActive": false
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "User updated successfully",
  "data": {
    "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
    "username": "john_doe_updated",
    "email": "john_new@example.com",
    "fullName": "John Doe Updated",
    "isActive": false,
    "createdAt": "2024-01-20T12:00:00Z"
  }
}
```

---

## 2.6 DELETE User

**Method:** `DELETE`  
**Endpoint:** `/api/v1/users/{id}`  
**Path Params:** `id` (Guid, required)

**Request:**
```
Không có body
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "User deleted successfully",
  "data": null
}
```

---

## 2.7 ASSIGN Roles to User

**Method:** `POST`  
**Endpoint:** `/api/v1/users/{id}/roles`  
**Path Params:** `id` (Guid, required)

**Request Body:**
```json
{
  "roleIds": [
    "2264fb32-ab1e-f111-ad11-00090ffe0001",
    "2164fb32-ab1e-f111-ad11-00090ffe0001"
  ]
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Roles assigned to user successfully",
  "data": null
}
```

**⚠️ Lưu ý:** THAY THẾ toàn bộ roles cũ

---

## 2.8 CHANGE Password

**Method:** `POST`  
**Endpoint:** `/api/v1/users/{id}/change-password`  
**Path Params:** `id` (Guid, required)

**Request Body:**
```json
{
  "currentPassword": "SecurePass123",
  "newPassword": "NewSecurePass456"
}
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Password changed successfully",
  "data": null
}
```

**Response 401 - Wrong Password:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Current password is incorrect"
  },
  "message": "Current password is incorrect",
  "data": null
}
```

---

## 🛠️ SETUP

```bash
cd ExamSystem
dotnet restore
dotnet ef database update
dotnet run
```

App: `http://localhost:5082`

---

## 📞 LIÊN HỆ

**GitHub:** [vinhdinhduc/ExamSystem_Server](https://github.com/vinhdinhduc/ExamSystem_Server)  
**Branch:** `feature/auth`

**Cập nhật:** 13/03/2026  
**Phiên bản:** v2.0  
**APIs:** 20/20 ✅

### Nhóm 3: Authentication System (JWT)

---

# 📡 NHÓM 3: AUTHENTICATION (4 APIs)

## 3.1 REGISTER (Đăng ký)

**Method:** `POST`  
**Endpoint:** `/api/v1/auth/register`

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
    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_in": 900,
    "user": {
      "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
      "username": "john_doe",
      "email": "john@example.com",
      "fullName": "John Doe",
      "isActive": true,
      "createdAt": "2024-01-20T12:00:00Z"
    }
  }
}
```

**Set-Cookie Header:**
```
refresh_token=dGhpc2lzYXJlZnJlc2h0b2tlbg==...; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Expires=Sun, 27 Jan 2024 12:00:00 GMT
```

**⚠️ Lưu ý:**
- Access token trả về trong response body
- Refresh token gửi qua HttpOnly Cookie (JavaScript KHÔNG thể đọc)
- Cookie tự động được browser gửi kèm mỗi request đến `/api/v1/auth/*`

**Response 400 - Duplicate Username:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "BUSINESS_ERROR",
    "reason": "Tên đăng nhập 'john_doe' đã được sử dụng"
  },
  "message": "Tên đăng nhập 'john_doe' đã được sử dụng",
  "data": null
}
```

**Response 400 - Validation:**
```json
{
  "statusCode": 400,
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [
      {
        "field": "Username",
        "message": "Username is required"
      },
      {
        "field": "Email",
        "message": "Invalid email format"
      },
      {
        "field": "Password",
        "message": "Password must be at least 6 characters"
      }
    ]
  },
  "message": "Dữ liệu không hợp lệ",
  "data": null
}
```

---

## 3.2 LOGIN (Đăng nhập)

**Method:** `POST`  
**Endpoint:** `/api/v1/auth/login`

**Request Body:**
```json
{
  "usernameOrEmail": "john_doe",
  "password": "SecurePass123"
}
```

**⚠️ Lưu ý:** `usernameOrEmail` có thể là username HOẶC email

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đăng nhập thành công",
  "data": {
    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_in": 900,
    "user": {
      "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
      "username": "john_doe",
      "email": "john@example.com",
      "fullName": "John Doe",
      "isActive": true,
      "createdAt": "2024-01-20T12:00:00Z"
    }
  }
}
```

**Set-Cookie Header:**
```
refresh_token=dGhpc2lzYXJlZnJlc2h0b2tlbjEyMzQ1Njc4OTA=; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Expires=Sun, 27 Jan 2024 12:00:00 GMT
```

**Response 401 - Invalid Credentials:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Tên đăng nhập hoặc mật khẩu không đúng"
  },
  "message": "Tên đăng nhập hoặc mật khẩu không đúng",
  "data": null
}
```

**Response 401 - Inactive Account:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Tài khoản đã bị khóa"
  },
  "message": "Tài khoản đã bị khóa",
  "data": null
}
```

---

## 3.3 REFRESH Token (Làm mới token)

**Method:** `POST`  
**Endpoint:** `/api/v1/auth/refresh`

**Request Body:**
```
KHÔNG CÓ BODY
```

**Cookie (tự động gửi bởi browser):**
```
refresh_token=dGhpc2lzYXJlZnJlc2h0b2tlbjEyMzQ1Njc4OTA=
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Làm mới token thành công",
  "data": {
    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expires_in": 900,
    "user": {
      "id": "8fa85f64-5717-4562-b3fc-2c963f66afab",
      "username": "john_doe",
      "email": "john@example.com",
      "fullName": "John Doe",
      "isActive": true,
      "createdAt": "2024-01-20T12:00:00Z"
    }
  }
}
```

**Set-Cookie Header (NEW refresh token):**
```
refresh_token=bmV3cmVmcmVzaHRva2VuMTIzNDU2Nzg5MA==; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Expires=Sun, 27 Jan 2024 14:00:00 GMT
```

**⚠️ Lưu ý:** 
- Refresh token cũ tự động bị revoke
- Trả về access token MỚI + refresh token MỚI (qua cookie)
- Client KHÔNG cần gửi refresh token trong body, cookie tự động gửi

**Response 401 - Missing Cookie:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại"
  },
  "message": "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại",
  "data": null
}
```

**Response 401 - Invalid Token:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại"
  },
  "message": "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại",
  "data": null
}
```

**Response 401 - Revoked Token:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Phiên đăng nhập đã bị thu hồi"
  },
  "message": "Phiên đăng nhập đã bị thu hồi",
  "data": null
}
```

**Response 401 - Expired Token:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại"
  },
  "message": "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại",
  "data": null
}
```

**Response 401 - Inactive User:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Tài khoản đã bị khóa"
  },
  "message": "Tài khoản đã bị khóa",
  "data": null
}
```

---

## 3.4 LOGOUT (Đăng xuất)

**Method:** `POST`  
**Endpoint:** `/api/v1/auth/logout`

**Request Body:**
```
KHÔNG CÓ BODY
```

**Cookie (tự động gửi bởi browser):**
```
refresh_token=dGhpc2lzYXJlZnJlc2h0b2tlbjEyMzQ1Njc4OTA=
```

**Response 200:**
```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đăng xuất thành công",
  "data": null
}
```

**Set-Cookie Header (xóa cookie):**
```
refresh_token=; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Expires=Thu, 01 Jan 1970 00:00:00 GMT
```

**⚠️ Lưu ý:**
- Refresh token bị revoke trong database
- Cookie tự động bị xóa
- Client KHÔNG cần gửi refresh token trong body

---

## 🔐 Protected Endpoints (Yêu cầu xác thực)

**TẤT CẢ endpoints sau đây yêu cầu Access Token:**

### Cách sử dụng Access Token

**Header:**
```
Authorization: Bearer {access_token}
```

**Ví dụ:**
```http
GET /api/v1/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### Danh sách Endpoints được bảo vệ

#### ✅ Roles (7 endpoints)
- `GET /api/v1/roles` - Lấy danh sách roles
- `GET /api/v1/roles/{id}` - Lấy role theo ID
- `GET /api/v1/roles/{id}/permissions` - Lấy role kèm permissions
- `POST /api/v1/roles` - Tạo role mới
- `PUT /api/v1/roles/{id}` - Cập nhật role
- `DELETE /api/v1/roles/{id}` - Xóa role
- `POST /api/v1/roles/{id}/permissions` - Gán permissions

#### ✅ Permissions (5 endpoints)
- `GET /api/v1/permissions` - Lấy danh sách permissions
- `GET /api/v1/permissions/{id}` - Lấy permission theo ID
- `POST /api/v1/permissions` - Tạo permission mới
- `PUT /api/v1/permissions/{id}` - Cập nhật permission
- `DELETE /api/v1/permissions/{id}` - Xóa permission

#### ✅ Users (8 endpoints)
- `GET /api/v1/users` - Lấy danh sách users
- `GET /api/v1/users/{id}` - Lấy user theo ID
- `GET /api/v1/users/{id}/roles` - Lấy user kèm roles
- `POST /api/v1/users` - Tạo user mới
- `PUT /api/v1/users/{id}` - Cập nhật user
- `DELETE /api/v1/users/{id}` - Xóa user
- `POST /api/v1/users/{id}/roles` - Gán roles
- `POST /api/v1/users/{id}/change-password` - Đổi password

---

### Xử lý lỗi xác thực

**401 - Chưa đăng nhập:**
```http
GET /api/v1/users
(KHÔNG có Authorization header)
```

**Response:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"
  },
  "message": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn",
  "data": null
}
```

---

**401 - Token hết hạn:**
```http
GET /api/v1/users
Authorization: Bearer {expired_access_token}
```

**Response:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"
  },
  "message": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn",
  "data": null
}
```

**Response Header:**
```
Token-Expired: true
```

**⚠️ Lưu ý:** Khi nhận `Token-Expired: true`, client nên:
1. Gọi `POST /api/v1/auth/refresh` để lấy access token mới
2. Retry request với access token mới
3. Nếu refresh failed → redirect to login

---

**401 - Token không hợp lệ:**
```http
GET /api/v1/users
Authorization: Bearer invalid_token
```

**Response:**
```json
{
  "statusCode": 401,
  "error": {
    "code": "UNAUTHORIZED",
    "reason": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"
  },
  "message": "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn",
  "data": null
}
```

---

## 🔒 JWT Token Details

### Access Token Claims
```json
{
  "nameid": "8fa85f64-5717-4562-b3fc-2c963f66afab",
  "unique_name": "john_doe",
  "email": "john@example.com",
  "fullName": "John Doe",
  "isActive": "True",
  "nbf": 1705752000,
  "exp": 1705755600,
  "iat": 1705752000,
  "iss": "ExamSystemAPI",
  "aud": "ExamSystemClient"
}
```

### Token Configuration
- **Access Token Lifetime:** 15 minutes (900 seconds)
- **Refresh Token Lifetime:** 7 days
- **Algorithm:** HMAC-SHA256
- **Issuer:** ExamSystemAPI
- **Audience:** ExamSystemClient

### Cookie Configuration
- **HttpOnly:** true (JavaScript không thể truy cập)
- **Secure:** true (chỉ gửi qua HTTPS)
- **SameSite:** Strict (ngăn CSRF)
- **Path:** /api/v1/auth (chỉ gửi đến auth endpoints)
- **Expires:** 7 days

---

## 🔑 Security Features

### Password Security
- ✅ ASP.NET Core Identity PasswordHasher
- ✅ Bcrypt-like hashing algorithm
- ✅ Salt generated per password
- ✅ Never store plain text passwords

### Token Security
- ✅ JWT signed with HMAC-SHA256
- ✅ Refresh token: 64-byte random (Base64)
- ✅ Token stored in database
- ✅ Token revocation on logout
- ✅ Expired token validation
- ✅ One-time use refresh tokens

### Best Practices
- ✅ Short-lived access tokens (60 min)
- ✅ Long-lived refresh tokens (7 days)
- ✅ Refresh token rotation (old token revoked)
- ✅ Account status validation (IsActive)
- ✅ ClockSkew = 0 (no time tolerance)

---

## 🧪 Testing Scenarios

### Scenario 1: Register → Use Access Token

**Step 1: Register**
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "username": "test_user",
  "email": "test@example.com",
  "password": "Test123456",
  "fullName": "Test User"
}
```

**Expected:** 
- Status 201
- Response body có `access_token` + `expires_in` + `user`
- Set-Cookie header có `refresh_token`

---

**Step 2: Use Access Token**
```http
GET /api/v1/users
Authorization: Bearer {access_token from step 1}
```

**Expected:** 
- Status 200
- Danh sách users

---

### Scenario 2: Login → Access Protected → Refresh → Logout

**Step 1: Login**
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "test_user",
  "password": "Test123456"
}
```

**Expected:**
- Status 200
- Response body có `access_token`
- Cookie `refresh_token` được set

---

**Step 2: Access protected endpoint**
```http
GET /api/v1/roles
Authorization: Bearer {access_token}
```

**Expected:**
- Status 200
- Danh sách roles

---

**Step 3: Wait 15 minutes (hoặc test với token hết hạn)**
```http
GET /api/v1/roles
Authorization: Bearer {expired_access_token}
```

**Expected:**
- Status 401
- Header `Token-Expired: true`
- Message: "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"

---

**Step 4: Refresh token**
```http
POST /api/v1/auth/refresh
(Cookie tự động gửi refresh_token)
```

**Expected:**
- Status 200
- Response body có `access_token` MỚI
- Cookie `refresh_token` MỚI được set
- Refresh token cũ bị revoke

---

**Step 5: Use new access token**
```http
GET /api/v1/roles
Authorization: Bearer {new_access_token}
```

**Expected:**
- Status 200
- Danh sách roles

---

**Step 6: Logout**
```http
POST /api/v1/auth/logout
(Cookie tự động gửi refresh_token)
```

**Expected:**
- Status 200
- Cookie `refresh_token` bị xóa
- Refresh token bị revoke trong database

---

**Step 7: Try to refresh with revoked token**
```http
POST /api/v1/auth/refresh
(Cookie chứa revoked refresh_token)
```

**Expected:**
- Status 401
- Message: "Phiên đăng nhập đã bị thu hồi"

---

### Scenario 3: Access without token

**Without Token:**
```http
GET /api/v1/users
```

**Expected:** 
- Status 401
- Message: "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"

---

**With Invalid Token:**
```http
GET /api/v1/users
Authorization: Bearer invalid_token_here
```

**Expected:**
- Status 401
- Message: "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"

---

## 🛠️ SETUP

```bash
cd ExamSystem
dotnet restore
dotnet ef database update
dotnet run
```

App: `http://localhost:5082`

---

## 📞 LIÊN HỆ

**GitHub:** [vinhdinhduc/ExamSystem_Server](https://github.com/vinhdinhduc/ExamSystem_Server)  
**Branch:** `feature/auth`

**Cập nhật:** 13/03/2026  
**Phiên bản:** v3.0  
**APIs:** 24/24 ✅
