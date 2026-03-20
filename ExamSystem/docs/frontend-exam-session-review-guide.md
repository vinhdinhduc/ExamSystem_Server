# Hướng dẫn frontend: API xem lại kết quả bài làm

## Endpoint

- **GET** `/api/v1/exam-sessions/{sessionId}/review`
- Yêu cầu: Bearer token
- Người dùng chỉ xem được session của chính mình.

## Response thành công

```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lấy kết quả bài làm thành công",
  "data": {
    "sessionId": "guid",
    "examId": "guid",
    "userId": "guid",
    "examTitle": "Đề thi thử SQL Server cơ bản",
    "score": 75,
    "isPassed": true,
    "totalCorrect": 3,
    "startedAt": "2026-03-19T09:00:00Z",
    "submittedAt": "2026-03-19T09:25:00Z",
    "questions": [
      {
        "questionId": "guid",
        "content": "Câu hỏi...",
        "explanation": "Giải thích...",
        "orderIndex": 1,
        "score": 1,
        "isCorrect": true,
        "selectedAnswerIds": [11],
        "correctAnswerIds": [11],
        "options": [
          {
            "id": 10,
            "content": "A",
            "imageUrl": null,
            "orderIndex": 0,
            "isSelected": false,
            "isCorrect": false
          },
          {
            "id": 11,
            "content": "B",
            "imageUrl": null,
            "orderIndex": 1,
            "isSelected": true,
            "isCorrect": true
          }
        ]
      }
    ]
  }
}
```

## Lưu ý nghiệp vụ

- Nếu đề thi có `showCorrectAnswer = false`:
  - `correctAnswerIds` trả về mảng rỗng
  - `options[].isCorrect` trả về `null`
- Nếu session chưa nộp:
  - API trả lỗi business: `Phiên thi chưa được nộp nên chưa thể xem kết quả`

## Lỗi thường gặp

- `401`: token không hợp lệ/hết hạn
- `403`: không đủ quyền truy cập
- `400`: session chưa nộp
- `401`: cố xem session của người khác
- `404`: session không tồn tại

## Gợi ý tích hợp FE

1. Sau khi submit thành công, lấy `sessionId` từ API submit.
2. Gọi endpoint review để render trang kết quả chi tiết.
3. Hiển thị:
   - trạng thái đúng/sai từng câu (`isCorrect`)
   - đáp án người dùng đã chọn (`isSelected` hoặc `selectedAnswerIds`)
   - đáp án đúng nếu backend cho phép (`showCorrectAnswer = true`).
