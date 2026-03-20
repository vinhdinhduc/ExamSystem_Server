# API Contract cho Frontend — Groups & Exam Assignment


---

## 2) Format response chuẩn

Backend trả theo mẫu `ApiResponse<T>`:

```json
{
  "statusCode": 200,
  "error": null,
  "message": "Thông báo",
  "data": {}
}
```

### 2.1 Response phân trang

```json
{
  "statusCode": 200,
  "error": null,
  "message": "Groups retrieved successfully",
  "data": {
    "meta": {
      "page": 1,
      "pageSize": 20,
      "pages": 2,
      "total": 25
    },
    "result": []
  }
}
```

### 2.2 Response lỗi validate (`400`)

```json
{
  "statusCode": 400,
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [
      { "field": "Code", "message": "Mã nhóm không được để trống" }
    ]
  },
  "message": "Validation failed",
  "data": null
}
```

### 2.3 Response không tìm thấy (`404`)

```json
{
  "statusCode": 404,
  "error": {
    "code": "NOT_FOUND",
    "resource": "Group with id '1'"
  },
  "message": "Group not found",
  "data": null
}
```

---

## 3) DTO dùng cho Groups

### 3.1 `GroupDto` (response)

```json
{
  "id": 1,
  "name": "Lớp 10A1",
  "code": "GRP10A1",
  "description": "Nhóm học sinh lớp 10A1",
  "createdByUserId": "00000000-0000-0000-0000-000000000000",
  "createdAt": "2026-03-18T10:00:00Z",
  "members": [
    {
      "groupId": 1,
      "userId": "00000000-0000-0000-0000-000000000000",
      "joinedAt": "2026-03-18T10:10:00Z"
    }
  ]
}
```

### 3.2 `GroupCreateDto` (request body)

```json
{
  "name": "Lớp 10A1",
  "code": "GRP10A1",
  "description": "Nhóm học sinh lớp 10A1",
  "createdByUserId": "00000000-0000-0000-0000-000000000000"
}
```

Validate:
- `name`: bắt buộc, max 100 ký tự
- `code`: bắt buộc, max 50, regex `^[A-Z0-9_]+$`
- `description`: optional, max 500
- `code` phải unique (backend check)

### 3.3 `GroupUpdateDto` (request body)

```json
{
  "name": "Lớp 10A1 - cập nhật",
  "code": "GRP10A1",
  "description": "Mô tả mới"
}
```

### 3.4 `AddGroupMemberDto` (request body)

```json
{
  "userId": "00000000-0000-0000-0000-000000000000"
}
```

---

## 4) Groups API

### 4.1 Lấy danh sách nhóm (phân trang)
- **GET** `/api/v1/groups?keyword=&page=1&pageSize=20`

Query params:
- `keyword` (optional, string)
- `page` (optional, int)
- `pageSize` (optional, int)

Trả về: `ApiResponse<PaginatedResult<GroupDto>>`

### 4.2 Lấy chi tiết nhóm
- **GET** `/api/v1/groups/{id}`

Trả về: `ApiResponse<GroupDto>`

### 4.3 Tạo nhóm
- **POST** `/api/v1/groups`
- Body: `GroupCreateDto`

Trả về: `ApiResponse<GroupDto>` (status `201`)

### 4.4 Cập nhật nhóm
- **PUT** `/api/v1/groups/{id}`
- Body: `GroupUpdateDto`

Trả về: `ApiResponse<GroupDto>`

### 4.5 Xóa nhóm
- **DELETE** `/api/v1/groups/{id}`

Trả về: `ApiResponse<object>` với `data = null`

### 4.6 Thêm thành viên vào nhóm
- **POST** `/api/v1/groups/{id}/members`
- Body: `AddGroupMemberDto`

Trả về: `ApiResponse<object>` với `data = null`

### 4.7 Xóa thành viên khỏi nhóm
- **DELETE** `/api/v1/groups/{id}/members/{userId}`

Trả về: `ApiResponse<object>` với `data = null`

---

## 5) API phân công đề thi

### 5.1 Phân công đề thi cho user hoặc group
- **POST** `/api/v1/exams/{id}/assign`
- Body:

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "groupId": null
}
```

hoặc

```json
{
  "userId": null,
  "groupId": 1
}
```

Lưu ý:
- Phải có **một trong hai**: `userId` hoặc `groupId`
- Không truyền đồng thời cả hai

Trả về: `ApiResponse<object>` với `data = null`

### 5.2 Lấy danh sách đề thi học sinh được giao
- **GET** `/api/v1/exams/student/{userId}/assigned`

Trả về: `ApiResponse<List<StudentAssignedExamDto>>`

```json
[
  {
    "examId": "00000000-0000-0000-0000-000000000000",
    "title": "Đề thi thử Toán cơ bản",
    "duration": 30,
    "passScore": 5,
    "startDate": "2026-03-18T00:00:00Z",
    "endDate": "2026-04-18T00:00:00Z",
    "status": 1,
    "assignedAt": "2026-03-18T10:00:00Z"
  }
]
```
