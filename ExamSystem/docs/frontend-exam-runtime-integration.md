# Frontend Integration — Exam Runtime APIs



## 2) Endpoint cần dùng

## 2.1 Danh sách đề được giao cho user hiện tại
- **GET** `/exams/assigned`
- Response `data`: `StudentAssignedExam[]`

Ví dụ:

```json
[
  {
    "examId": "guid",
    "title": "Đề thi thử SQL Server cơ bản",
    "duration": 30,
    "passScore": 50,
    "startDate": "2026-03-19T10:46:00",
    "endDate": "2026-03-30T10:46:00",
    "status": 1,
    "assignedAt": "2026-03-19T03:21:13"
  }
]
```

---

## 2.2 Lấy chi tiết đề thi
- **GET** `/exams/{examId}`
- Mục đích: lấy metadata đề thi (`title`, `duration`, `passScore`, ...)

> Lưu ý: trường `questions` trong response này chỉ là danh sách `ExamQuestionDto` (id liên kết), không có nội dung đáp án.

---

## 2.3 Lấy danh sách câu hỏi chi tiết theo đề (mới)
- **GET** `/exams/{examId}/questions/details`
- Mục đích: lấy data để render trang làm bài (content câu hỏi + options)

Response `data`: `ExamQuestionDetailDto[]`

```json
[
  {
    "examQuestionId": 1,
    "examId": "guid",
    "questionId": "guid",
    "content": "Câu hỏi ...",
    "imageUrl": null,
    "questionType": 0,
    "difficultyLevel": 1,
    "tags": null,
    "explanation": "...",
    "orderIndex": 1,
    "score": 5,
    "options": [
      { "id": 10, "content": "Đáp án A", "imageUrl": null, "orderIndex": 0 },
      { "id": 11, "content": "Đáp án B", "imageUrl": null, "orderIndex": 1 }
    ]
  }
]
```

> Backend không trả `isCorrect` để tránh lộ đáp án khi học sinh đang làm bài.

---

## 2.4 Bắt đầu làm bài
Hỗ trợ 2 route:

1) **POST** `/exam-sessions/{examId}/start`
2) **POST** `/exam-sessions/start?examId={examId}` (compat route)

Body:

```json
{
  "userId": "guid-optional",
  "accessCode": null
}
```

- `userId` có thể bỏ qua, backend sẽ lấy từ JWT.

Response `data`: `StartExamResponseDto`

```json
{
  "sessionId": "guid",
  "startedAt": "2026-03-19T08:00:00Z",
  "expiresAt": "2026-03-19T08:30:00Z",
  "attemptNumber": 1,
  "questions": [
    {
      "questionId": "guid",
      "orderIndex": 1,
      "answerIds": [10, 11, 12, 13]
    }
  ]
}
```

---

## 2.5 Autosave đáp án
- **PUT** `/exam-sessions/{sessionId}/autosave`

Body:

```json
{
  "userId": "guid-optional",
  "questionId": "guid",
  "answerIds": [11]
}
```

---

## 2.6 Nộp bài
- **POST** `/exam-sessions/{sessionId}/submit`

Body:

```json
{
  "userId": "guid-optional"
}
```

Response `data`:

```json
{
  "sessionId": "guid",
  "score": 75,
  "isPassed": true,
  "totalCorrect": 3,
  "submittedAt": "2026-03-19T08:29:00Z",
  "status": 1
}
```

---

## 3) Mapping cho FE hiện tại

## 3.1 Những URL cần sửa

- Đúng: `GET /exams/assigned`
- Đúng: `GET /exams/{id}`
- Mới cần dùng để render câu hỏi: `GET /exams/{id}/questions/details`
- Đúng: `POST /exam-sessions/{examId}/start`
- Compat: `POST /exam-sessions/start?examId={examId}`

## 3.2 Gợi ý flow cho `DoExamPage`

1. gọi `GET /exams/{id}`
2. gọi `GET /exams/{id}/questions/details`
3. gọi `POST /exam-sessions/{id}/start`
4. render UI dựa trên `questions/details` + order từ session
5. autosave theo từng câu
6. submit

---

## 4) Lỗi thường gặp

- `404 /exams/assigned`: backend cũ chưa có route compat.
- `404 /exam-sessions/.../start`: thiếu `ExamSessionsController`.
- Có `examDetail.questions` nhưng không có đáp án: vì endpoint `/exams/{id}` chỉ trả liên kết `ExamQuestionDto`, không phải dữ liệu hiển thị chi tiết.
