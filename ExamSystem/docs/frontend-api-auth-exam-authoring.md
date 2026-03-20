# Frontend API Spec — Auth & Exam Authoring

Tài liệu này mô tả endpoint, dữ liệu request/response cần thiết để frontend tích hợp nhanh.

## 1) Thông tin chung

- Base URL local: `https://localhost:7238`
- API version prefix: `/api/v1`
- Response chuẩn:

```json
{
  "statusCode": 200,
  "error": null,
  "message": "...",
  "data": {}
}
```

- Với endpoint cần đăng nhập, gửi header:
  - `Authorization: Bearer <access_token>`

---

## 2) Auth API

Base route: `/api/v1/auth`

### 2.1 Đăng ký
- **POST** `/api/v1/auth/register`
- Body (`application/json`):

```json
{
  "email": "user@example.com",
  "password": "123456",
  "username": "optional_username",
  "fullName": "optional_full_name"
}
```

- Bắt buộc: `email`, `password`
- `username`, `fullName` có thể bỏ trống (backend tự sinh)

### 2.2 Đăng nhập
- **POST** `/api/v1/auth/login`
- Body (`application/json`):

```json
{
  "usernameOrEmail": "user@example.com",
  "password": "123456"
}
```

- Response `data`:

```json
{
  "access_token": "...",
  "refresh_token": "...",
  "expires_in": 900,
  "user": {
    "id": "guid",
    "username": "...",
    "fullName": "...",
    "email": "...",
    "isActive": true,
    "createdAt": "...",
    "roles": ["Admin"]
  }
}
```

### 2.3 Refresh token
- **POST** `/api/v1/auth/refresh`
- Không cần body
- Dùng cookie `refresh_token` do backend set.

### 2.4 Logout
- **POST** `/api/v1/auth/logout`
- Không cần body
- Backend sẽ revoke token và xóa cookie `refresh_token`.

### 2.5 Verify email
- **GET** `/api/v1/auth/verify-email?token=...`

### 2.6 Quên mật khẩu
- **POST** `/api/v1/auth/forgot-password`

```json
{
  "email": "user@example.com"
}
```

### 2.7 Đặt lại mật khẩu bằng OTP
- **POST** `/api/v1/auth/reset-password`

```json
{
  "email": "user@example.com",
  "otp": "123456",
  "newPassword": "new_password",
  "confirmPassword": "new_password"
}
```

---

## 3) Exam Authoring API (Gemini + Import file)

Base route: `/exam-authoring`

> Yêu cầu đăng nhập + permission `EXAM_CREATE`.

### 3.1 Sinh đề từ Gemini
- **POST** `/exam-authoring/generate`
- Body (`application/json`):

```json
{
  "subjectId": 1,
  "createdByUserId": "guid",
  "title": "Đề thi Sinh học 12",
  "description": "Đề luyện tập",
  "instructions": "Chọn đáp án đúng nhất",
  "questionCount": 20,
  "difficultyLevel": 2,
  "duration": 45,
  "passScore": 5,
  "maxAttempts": 2,
  "shuffleQuestions": true,
  "shuffleAnswers": true,
  "showResultAfter": true,
  "showCorrectAnswer": false,
  "status": 0,
  "startDate": null,
  "endDate": null,
  "accessCode": null,
  "additionalPrompt": "Tập trung chương di truyền",
  "saveToDatabase": true
}
```

- Trường bắt buộc chính:
  - `subjectId`, `createdByUserId`, `title`, `questionCount`, `duration`, `passScore`, `maxAttempts`, `status`
- Validate chính:
  - `questionCount` từ `1` đến `200`
  - `duration > 0`, `maxAttempts > 0`, `passScore >= 0`
  - `status` trong `0..2`
  - `endDate >= startDate` nếu có cả hai

### 3.2 Import đề từ file
- **POST** `/api/v1/exam-authoring/import`
- Content-Type: `multipart/form-data`
- Form fields:
  - `subjectId` (int)
  - `createdByUserId` (guid)
  - `title` (string)
  - `description` (optional)
  - `instructions` (optional)
  - `duration` (int)
  - `passScore` (decimal)
  - `maxAttempts` (int)
  - `shuffleQuestions` (bool)
  - `shuffleAnswers` (bool)
  - `showResultAfter` (bool)
  - `showCorrectAnswer` (bool)
  - `status` (byte)
  - `startDate` (optional)
  - `endDate` (optional)
  - `accessCode` (optional)
  - `saveToDatabase` (bool, optional, default `true`)
  - `file` (**bắt buộc**)

- Định dạng file hỗ trợ:
  - `.xlsx`, `.xls`, `.csv`, `.txt`

### 3.3 Cấu trúc file import khuyến nghị

Header nên dùng (không phân biệt hoa thường, bỏ khoảng trắng vẫn map được):

- `questionContent` (hoặc `content`, `cauhoi`)
- `optionA`
- `optionB`
- `optionC`
- `optionD`
- `correctOption` (hoặc `correct`, `dapan`) — nhận `A/B/C/D`, `1/2/3/4`, hoặc text đáp án
- `explanation` (optional)
- `difficultyLevel` (optional)
- `score` (optional)
- `questionType` (optional)
- `orderIndex` (optional)

Ví dụ dòng CSV:

```csv
questionContent,optionA,optionB,optionC,optionD,correctOption,explanation,difficultyLevel,score,questionType,orderIndex
"2+2=?",3,4,5,6,B,"Phép cộng cơ bản",1,1,0,1
```

---

## 4) Error format để FE xử lý

### Validation lỗi (`400`)

```json
{
  "statusCode": 400,
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [
      { "field": "Title", "message": "Tiêu đề đề thi không được để trống" }
    ]
  },
  "message": "Dữ liệu không hợp lệ",
  "data": null
}
```

### Unauthorized (`401`)

```json
{
  "statusCode": 401,
  "error": { "code": "UNAUTHORIZED", "reason": "..." },
  "message": "...",
  "data": null
}
```

### Forbidden (`403`)

```json
{
  "statusCode": 403,
  "error": { "code": "FORBIDDEN", "reason": "..." },
  "message": "...",
  "data": null
}
```
