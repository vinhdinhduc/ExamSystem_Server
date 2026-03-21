# Hướng dẫn Frontend: Generate đề thi bằng AI và cho phép chỉnh sửa trước khi lưu

## Mục tiêu

Cho phép flow:
1. FE gọi AI để sinh nháp đề thi.
2. User chỉnh sửa câu hỏi/đáp án trên UI.
3. FE gửi nháp đã chỉnh sửa để backend lưu thành đề thi thật.

---

## Các endpoint cần dùng

### 1) Generate nháp bằng AI (không lưu DB)

- **POST** `/api/v1/exam-authoring/generate`
- Auth: Bearer token
- Permission: `EXAM_CREATE`

Gợi ý request:

```json
{
  "subjectId": 1,
  "createdByUserId": "00000000-0000-0000-0000-000000000000",
  "title": "Đề kiểm tra SQL cơ bản",
  "description": "Ôn tập truy vấn SQL",
  "instructions": "Chọn đáp án đúng nhất",
  "questionCount": 10,
  "difficultyLevel": 1,
  "duration": 15,
  "passScore": 5,
  "maxAttempts": 1,
  "shuffleQuestions": false,
  "shuffleAnswers": true,
  "showResultAfter": true,
  "showCorrectAnswer": true,
  "status": 0,
  "startDate": null,
  "endDate": null,
  "accessCode": null,
  "additionalPrompt": "Tập trung câu hỏi SELECT/JOIN",
  "saveToDatabase": false
}
```

> Quan trọng: đặt `saveToDatabase = false` để backend chỉ trả nháp, chưa lưu.

---

### 2) Lưu nháp đã chỉnh sửa thành đề thi

- **POST** `/api/v1/exam-authoring/save-draft`
- Auth: Bearer token
- Permission: `EXAM_CREATE`

Request body:

```json
{
  "subjectId": 1,
  "createdByUserId": "00000000-0000-0000-0000-000000000000",
  "source": "gemini-edited",
  "draft": {
    "title": "Đề SQL đã chỉnh sửa",
    "description": "Bản đã chỉnh sửa bởi giáo viên",
    "instructions": "Đọc kỹ câu hỏi trước khi chọn",
    "duration": 20,
    "passScore": 6,
    "maxAttempts": 2,
    "shuffleQuestions": false,
    "shuffleAnswers": true,
    "showResultAfter": true,
    "showCorrectAnswer": true,
    "status": 0,
    "startDate": null,
    "endDate": null,
    "accessCode": null,
    "questions": [
      {
        "content": "Câu hỏi 1...",
        "explanation": "Giải thích...",
        "questionType": 0,
        "difficultyLevel": 1,
        "score": 1,
        "orderIndex": 1,
        "options": [
          { "content": "A", "isCorrect": true, "orderIndex": 0, "imageUrl": null },
          { "content": "B", "isCorrect": false, "orderIndex": 1, "imageUrl": null }
        ]
      }
    ]
  }
}
```

Response thành công (rút gọn):

```json
{
  "statusCode": 200,
  "error": null,
  "message": "Lưu đề thi từ nháp đã chỉnh sửa thành công",
  "data": {
    "examId": "...",
    "source": "gemini-edited",
    "totalQuestions": 10,
    "draft": { }
  }
}
```

---

## Cấu trúc dữ liệu FE nên dùng

Nên lưu trạng thái editor theo `ExamDraftDto` để gửi thẳng cho endpoint `save-draft`:

- `title`, `description`, `instructions`
- `duration`, `passScore`, `maxAttempts`
- `shuffleQuestions`, `shuffleAnswers`, `showResultAfter`, `showCorrectAnswer`
- `status`, `startDate`, `endDate`, `accessCode`
- `questions[]`
  - `content`, `explanation`, `questionType`, `difficultyLevel`, `score`, `orderIndex`
  - `options[]`: `content`, `isCorrect`, `orderIndex`, `imageUrl`

---

## Flow UI đề xuất

1. User nhập thông tin + prompt AI.
2. FE gọi `generate` với `saveToDatabase=false`.
3. Render màn hình editor từ `data.draft`.
4. User sửa nội dung câu hỏi/đáp án, thêm/xóa/sắp xếp.
5. Bấm `Lưu đề thi` -> FE gọi `save-draft`.
6. Thành công: điều hướng tới trang preview theo `examId` trả về.

---

## Gợi ý service cho frontend

```ts
export interface SaveExamDraftRequest {
  subjectId: number
  createdByUserId: string
  source?: string
  draft: ExamDraft
}

saveDraft: async (payload: SaveExamDraftRequest) => {
  const response = await axiosClient.post('/exam-authoring/save-draft', payload)
  return response.data.data
}
```

---

## Lỗi thường gặp

- `400 Dữ liệu không hợp lệ`:
  - thiếu `draft.title`
  - `draft.questions` rỗng
  - `duration <= 0` hoặc `maxAttempts <= 0`
- `401/403`: thiếu token hoặc thiếu quyền `EXAM_CREATE`.

---

## Ghi chú tương thích

- Endpoint `generate` và `import` vẫn dùng bình thường.
- `save-draft` chỉ là bước mới để lưu bản đã chỉnh sửa từ FE.
