# Hướng dẫn FE: Xem kết quả bài làm theo vai trò

## 1) Học sinh xem kết quả đã làm

### Endpoint
- **GET** `/api/v1/exam-sessions/my-results`
- Auth: Bearer token

### Response `data`
```json
[
  {
    "sessionId": "guid",
    "examId": "guid",
    "examTitle": "Đề thi SQL",
    "score": 75,
    "isPassed": true,
    "totalCorrect": 15,
    "submittedAt": "2026-03-20T09:00:00Z",
    "status": 1,
    "attemptNumber": 1
  }
]
```

### Xem chi tiết 1 bài đã làm
- **GET** `/api/v1/exam-sessions/{sessionId}/review`
- Trả về từng câu hỏi + đáp án đã chọn + đáp án đúng (nếu đề cho phép hiển thị đáp án đúng).

---

## 2) Giáo viên xem tình trạng học sinh đã làm/chưa làm

### Endpoint
- **GET** `/api/v1/exam-sessions/teacher/assigned-results`
- Auth: Bearer token
- Yêu cầu quyền: `EXAM_SESSION_VIEW`
- Optional query: `examId`
  - Ví dụ: `/api/v1/exam-sessions/teacher/assigned-results?examId={examId}`

### Response `data`
```json
[
  {
    "examId": "guid",
    "examTitle": "Đề thi giữa kỳ",
    "totalAssigned": 30,
    "totalSubmitted": 18,
    "totalNotSubmitted": 12,
    "students": [
      {
        "userId": "guid",
        "fullName": "Nguyễn Văn A",
        "email": "a@example.com",
        "isSubmitted": true,
        "sessionId": "guid",
        "score": 82.5,
        "isPassed": true,
        "submittedAt": "2026-03-20T08:30:00Z",
        "attempts": 1
      },
      {
        "userId": "guid",
        "fullName": "Trần Thị B",
        "email": "b@example.com",
        "isSubmitted": false,
        "sessionId": null,
        "score": null,
        "isPassed": null,
        "submittedAt": null,
        "attempts": 0
      }
    ]
  }
]
```

---

## 3) Gợi ý tích hợp UI

### Màn học sinh
1. Gọi `my-results` để render danh sách kết quả.
2. Chọn 1 dòng -> gọi `review` để xem chi tiết câu hỏi.

### Màn giáo viên
1. Gọi `teacher/assigned-results` (có thể filter theo `examId`).
2. Hiển thị số liệu tổng: đã làm/chưa làm.
3. Bảng học sinh: dùng `isSubmitted` để tô màu trạng thái.
4. Với học sinh đã làm, có `sessionId` -> gọi `review` nếu cần xem chi tiết bài.
