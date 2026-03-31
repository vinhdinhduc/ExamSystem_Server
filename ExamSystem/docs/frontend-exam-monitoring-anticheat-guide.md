# Hướng dẫn FE: Save Progress, Submit, Anti-cheat, Realtime theo dõi thi

## 1) Tổng quan

Backend đã hỗ trợ các API runtime mới tại prefix:
- `/api/v1/exam/*`

Mục tiêu:
1. Auto-save đáp án + vị trí câu hiện tại.
2. Nộp bài chủ động.
3. Ghi nhận vi phạm anti-cheat và auto force-submit khi >= 3.
4. Realtime cho giáo viên/admin theo dõi tiến độ.

---

## 2) API cần gọi

## 2.1 Save progress
- **POST** `/api/v1/exam/save-progress`
- Auth: Bearer token

Request:

```json
{
  "sessionId": "guid",
  "userId": "guid-optional",
  "questionId": "guid",
  "answerIds": [11],
  "currentQuestionIndex": 4
}
```

Response `data`:

```json
{
  "sessionId": "guid",
  "currentQuestionIndex": 4,
  "violationCount": 0,
  "lastSavedAt": "2026-03-23T03:30:10.000Z",
  "status": 0,
  "isAutoSubmitted": false
}
```

Ý nghĩa `status`:
- `0`: in-progress
- `1`: submitted
- `2`: timed-out auto submit
- `3`: force-submitted do vi phạm

> Nếu đã hết giờ, API có thể trả `isAutoSubmitted = true`.

---

## 2.2 Submit bài
- **POST** `/api/v1/exam/submit`
- Auth: Bearer token

Request:

```json
{
  "sessionId": "guid",
  "userId": "guid-optional"
}
```

Response `data`:

```json
{
  "sessionId": "guid",
  "score": 8.5,
  "isPassed": true,
  "totalCorrect": 17,
  "submittedAt": "2026-03-23T03:45:00.000Z",
  "status": 1
}
```

### Công thức điểm
Mỗi câu có `score` riêng.

Backend tính:
- `score = (tổng điểm câu đúng / tổng điểm đề) * 10`

---

## 2.3 Report vi phạm anti-cheat
- **POST** `/api/v1/exam/violation`
- Auth: Bearer token

Request:

```json
{
  "sessionId": "guid",
  "userId": "guid-optional",
  "type": "TAB_SWITCH",
  "currentQuestionIndex": 4
}
```

`type` hợp lệ:
- `TAB_SWITCH`
- `COPY`
- `PASTE`
- `EXIT_FULLSCREEN`
- `DEVTOOLS`

Response `data`:

```json
{
  "sessionId": "guid",
  "violationCount": 3,
  "isForceSubmitted": true,
  "status": 3,
  "submittedAt": "2026-03-23T03:33:20.000Z"
}
```

Quy tắc:
- mỗi lần gọi tăng `violationCount`
- `violationCount >= 3` => tự động nộp (`status = 3`)

---

## 3) Realtime cho giáo viên/admin

Backend dùng SignalR hub:
- `/hubs/exam-monitoring`

Event phát từ server:
- `student_progress`
- `student_submit`

## 3.1 Join room
Client gọi methods:
- `JoinExamRoom(examId)` để theo dõi 1 đề cụ thể
- `JoinMonitoringRoom()` để theo dõi tổng

## 3.2 Payload event
### `student_progress`
```json
{
  "sessionId": "guid",
  "examId": "guid",
  "userId": "guid",
  "action": "save_progress",
  "currentQuestionIndex": 4,
  "violationCount": 1,
  "remainingSeconds": 1200,
  "progressPercent": 45.5,
  "status": 0
}
```

### `student_submit`
```json
{
  "sessionId": "guid",
  "examId": "guid",
  "userId": "guid",
  "reason": "submitted",
  "submittedAt": "2026-03-23T03:45:00.000Z",
  "score": 8.5,
  "isPassed": true,
  "status": 1
}
```

`reason` có thể là:
- `submitted`
- `timed_out`
- `force_submitted`

---

## 4) Gợi ý tích hợp FE

1. Trong trang làm bài, mỗi lần đổi câu hoặc chọn đáp án:
   - gọi `save-progress` (debounce 500-1000ms).
2. Khi user vi phạm (blur/tab/fullscreen/devtools):
   - gọi `violation`.
3. Khi bấm nộp:
   - gọi `submit`.
4. Dashboard giáo viên/admin:
   - connect hub, join room, subscribe 2 events để update realtime.

---

## 5) Ví dụ service (TypeScript)

```ts
saveProgress: (payload: SaveExamProgressRequest) =>
  axiosClient.post('/exam/save-progress', payload),

submitExam: (payload: { sessionId: string; userId?: string }) =>
  axiosClient.post('/exam/submit', payload),

reportViolation: (payload: ExamViolationRequest) =>
  axiosClient.post('/exam/violation', payload),
```

---

## 6) Lưu ý

- `userId` có thể để FE không gửi, backend sẽ resolve từ JWT.
- Nếu nhận `status = 2 hoặc 3`, FE cần khóa màn hình làm bài và chuyển sang trang kết quả.
- FE không nên tự tính điểm chính thức; dùng kết quả từ API `submit`/`review`.
