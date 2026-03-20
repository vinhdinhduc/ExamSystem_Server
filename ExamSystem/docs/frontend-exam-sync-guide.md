# Hướng dẫn Frontend dùng endpoint đồng bộ câu hỏi đề thi

## Endpoint mới

- **PUT** ` /api/v1/exams/{examId}/questions/sync `
- Auth: Bearer token
- Mục đích: đồng bộ toàn bộ danh sách câu hỏi của đề thi trong 1 lần gọi (thêm mới, cập nhật thứ tự/điểm, xóa câu bị bỏ).

## Request body

```json
{
  "items": [
    {
      "questionId": "2ed47656-a38a-458b-bdae-1d2276e38c6d",
      "orderIndex": 1,
      "score": 1
    },
    {
      "questionId": "11111111-2222-3333-4444-555555555555",
      "orderIndex": 2,
      "score": 1
    }
  ]
}
```

## Response thành công

```json
{
  "statusCode": 200,
  "error": null,
  "message": "Đồng bộ danh sách câu hỏi đề thi thành công",
  "data": null
}
```

## Vì sao cần endpoint này

Khi sửa đề thi, nếu frontend gọi lại `POST /exams/{id}/questions` cho toàn bộ danh sách đã chọn sẽ dễ lỗi:
- `Câu hỏi đã tồn tại trong đề thi`

Endpoint `sync` giúp tránh lỗi này vì backend tự xử lý diff:
- câu mới: thêm
- câu cũ bỏ chọn: xóa
- câu còn lại: cập nhật `orderIndex/score`

## Cập nhật `examService.ts`

Thêm hàm:

```ts
syncExamQuestions: async (examId: string, items: ExamQuestionCreatePayload[]): Promise<void> => {
  await axiosClient.put(`/exams/${examId}/questions/sync`, {
    items: items.map((item, index) => ({
      questionId: item.questionId,
      orderIndex: item.orderIndex ?? index + 1,
      score: item.score ?? 1,
    })),
  })
}
```

## Cập nhật flow ở `ExamBuilderPage` (mode edit)

Trong nhánh `isEdit`:
1. Gọi `updateExam({ id, payload })`
2. Sau khi thành công, gọi `syncExamQuestions(id, examQuestionItems)`
3. Không gọi lại `saveExamQuestions` kiểu loop POST như trước.

Pseudo:

```ts
if (isEdit && id) {
  const result = await dispatch(updateExam({ id, payload }))
  if (updateExam.fulfilled.match(result)) {
    await examService.syncExamQuestions(id, examQuestionItems)
    toast.success("Cập nhật đề thi thành công")
    navigate(`/exams/${id}/preview`)
  }
}
```
