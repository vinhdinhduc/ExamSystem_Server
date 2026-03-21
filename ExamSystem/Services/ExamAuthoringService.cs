using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using ExamSystem.Common;
using ExamSystem.Data;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExamSystem.Services;

public class ExamAuthoringService : IExamAuthoringService
{
    private readonly ExamSystemDbContext _context;
    private readonly GeminiSettings _geminiSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExamAuthoringService(
        ExamSystemDbContext context,
        IOptions<GeminiSettings> geminiSettings,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _geminiSettings = geminiSettings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExamAuthoringResultDto> GenerateWithGeminiAsync(GenerateExamWithGeminiRequestDto dto)
    {
        await EnsureSubjectAndUserAsync(dto.SubjectId, dto.CreatedByUserId);
        EnsureGeminiSettings();

        var draftQuestions = await GenerateQuestionsByGeminiAsync(dto);

        var draft = new ExamDraftDto(
            dto.Title,
            dto.Description,
            dto.Instructions,
            dto.Duration,
            dto.PassScore,
            dto.MaxAttempts,
            dto.ShuffleQuestions,
            dto.ShuffleAnswers,
            dto.ShowResultAfter,
            dto.ShowCorrectAnswer,
            dto.Status,
            dto.StartDate,
            dto.EndDate,
            dto.AccessCode,
            draftQuestions);

        Guid? examId = null;
        if (dto.SaveToDatabase)
        {
            examId = await SaveDraftAsExamAsync(dto.SubjectId, dto.CreatedByUserId, draft);
        }

        return new ExamAuthoringResultDto(examId, "gemini", draft.Questions.Count, draft);
    }

    public async Task<ExamAuthoringResultDto> ImportFromFileAsync(ImportExamFromFileRequestDto dto)
    {
        await EnsureSubjectAndUserAsync(dto.SubjectId, dto.CreatedByUserId);

        var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();

        var draftQuestions = extension switch
        {
            ".xlsx" or ".xls" => await ParseExcelAsync(dto.File),
            ".csv" or ".txt" => await ParseCsvAsync(dto.File),
            _ => throw new InvalidOperationException("Chỉ hỗ trợ file Excel (.xlsx, .xls) hoặc CSV/TXT")
        };

        if (draftQuestions.Count == 0)
        {
            throw new InvalidOperationException("Không có câu hỏi hợp lệ trong file import");
        }

        var draft = new ExamDraftDto(
            dto.Title,
            dto.Description,
            dto.Instructions,
            dto.Duration,
            dto.PassScore,
            dto.MaxAttempts,
            dto.ShuffleQuestions,
            dto.ShuffleAnswers,
            dto.ShowResultAfter,
            dto.ShowCorrectAnswer,
            dto.Status,
            dto.StartDate,
            dto.EndDate,
            dto.AccessCode,
            draftQuestions);

        Guid? examId = null;
        if (dto.SaveToDatabase)
        {
            examId = await SaveDraftAsExamAsync(dto.SubjectId, dto.CreatedByUserId, draft);
        }

        return new ExamAuthoringResultDto(examId, "file-import", draft.Questions.Count, draft);
    }

    public async Task<ExamAuthoringResultDto> SaveDraftAsync(SaveExamDraftRequestDto dto)
    {
        await EnsureSubjectAndUserAsync(dto.SubjectId, dto.CreatedByUserId);

        if (dto.Draft.Questions == null || dto.Draft.Questions.Count == 0)
        {
            throw new InvalidOperationException("Nháp đề thi phải có ít nhất 1 câu hỏi");
        }

        var normalizedQuestions = NormalizeQuestions(dto.Draft.Questions);

        var normalizedDraft = dto.Draft with
        {
            Questions = normalizedQuestions
        };

        var examId = await SaveDraftAsExamAsync(dto.SubjectId, dto.CreatedByUserId, normalizedDraft);

        return new ExamAuthoringResultDto(
            examId,
            string.IsNullOrWhiteSpace(dto.Source) ? "edited-draft" : dto.Source,
            normalizedDraft.Questions.Count,
            normalizedDraft);
    }

    private async Task EnsureSubjectAndUserAsync(int subjectId, Guid createdByUserId)
    {
        var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == subjectId);
        if (!subjectExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy môn học với id '{subjectId}'");
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == createdByUserId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy người dùng với id '{createdByUserId}'");
        }
    }

    private void EnsureGeminiSettings()
    {
        if (string.IsNullOrWhiteSpace(_geminiSettings.ApiKey))
        {
            throw new InvalidOperationException("Thiếu GeminiSettings:ApiKey trong cấu hình");
        }
    }

    private async Task<List<ExamQuestionDraftDto>> GenerateQuestionsByGeminiAsync(GenerateExamWithGeminiRequestDto dto)
    {
        var prompt = BuildGeminiPrompt(dto);

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json"
            }
        };

        var client = _httpClientFactory.CreateClient();
        var model = string.IsNullOrWhiteSpace(_geminiSettings.Model) ? "gemini-1.5-flash" : _geminiSettings.Model;
        var baseUrl = string.IsNullOrWhiteSpace(_geminiSettings.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : _geminiSettings.BaseUrl.TrimEnd('/');

        var response = await client.PostAsJsonAsync(
            $"{baseUrl}/v1beta/models/{model}:generateContent?key={_geminiSettings.ApiKey}",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gemini API lỗi: {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var rawText = ExtractGeminiText(responseJson);
        var jsonText = ExtractJson(rawText);

        var questions = ParseQuestionsFromJson(jsonText);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Gemini không trả về câu hỏi hợp lệ");
        }

        return NormalizeQuestions(questions);
    }

    private static string BuildGeminiPrompt(GenerateExamWithGeminiRequestDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là chuyên gia tạo câu hỏi trắc nghiệm.");
        sb.AppendLine("Hãy tạo bộ câu hỏi đúng định dạng JSON, KHÔNG thêm markdown, KHÔNG thêm giải thích ngoài JSON.");
        sb.AppendLine("JSON phải có dạng:");
        sb.AppendLine("{\"questions\":[{\"content\":\"...\",\"explanation\":\"...\",\"questionType\":0,\"difficultyLevel\":1,\"score\":1,\"orderIndex\":1,\"options\":[{\"content\":\"...\",\"isCorrect\":false,\"orderIndex\":0}]}]}");
        sb.AppendLine();
        sb.AppendLine($"Tiêu đề đề thi: {dto.Title}");
        sb.AppendLine($"Mô tả: {dto.Description}");
        sb.AppendLine($"Số câu hỏi: {dto.QuestionCount}");
        sb.AppendLine($"Mức độ khó chung: {dto.DifficultyLevel}");
        sb.AppendLine("Mỗi câu có tối thiểu 4 đáp án và ít nhất 1 đáp án đúng.");

        if (!string.IsNullOrWhiteSpace(dto.AdditionalPrompt))
        {
            sb.AppendLine($"Yêu cầu bổ sung: {dto.AdditionalPrompt}");
        }

        return sb.ToString();
    }

    private static string ExtractGeminiText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);

        var root = document.RootElement;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response không có candidates");
        }

        var first = candidates[0];
        var text = first.GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini response không có nội dung text");
        }

        return text;
    }

    private static string ExtractJson(string rawText)
    {
        var trimmed = rawText.Trim();

        if (trimmed.StartsWith("```") && trimmed.EndsWith("```"))
        {
            trimmed = trimmed.Trim('`').Trim();
            if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[4..].Trim();
            }
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }

        throw new InvalidOperationException("Không trích xuất được JSON từ Gemini response");
    }

    private static List<ExamQuestionDraftDto> ParseQuestionsFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        JsonElement questionsElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("questions", out var foundQuestions))
        {
            questionsElement = foundQuestions;
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            questionsElement = root;
        }
        else
        {
            throw new InvalidOperationException("JSON phải là object có trường questions hoặc là mảng câu hỏi");
        }

        if (questionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("questions phải là mảng");
        }

        var results = new List<ExamQuestionDraftDto>();
        foreach (var questionEl in questionsElement.EnumerateArray())
        {
            var content = questionEl.GetProperty("content").GetString() ?? string.Empty;
            var explanation = questionEl.TryGetProperty("explanation", out var expEl) ? expEl.GetString() : null;
            var questionType = (byte)(questionEl.TryGetProperty("questionType", out var qTypeEl) ? qTypeEl.GetInt32() : 0);
            var difficultyLevel = (byte)(questionEl.TryGetProperty("difficultyLevel", out var diffEl) ? diffEl.GetInt32() : 1);
            var score = questionEl.TryGetProperty("score", out var scoreEl) ? scoreEl.GetDecimal() : 1m;
            var orderIndex = questionEl.TryGetProperty("orderIndex", out var orderEl) ? orderEl.GetInt32() : results.Count + 1;

            var options = new List<ExamOptionDraftDto>();
            if (questionEl.TryGetProperty("options", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var optionEl in optionsEl.EnumerateArray())
                {
                    var optContent = optionEl.GetProperty("content").GetString() ?? string.Empty;
                    var isCorrect = optionEl.TryGetProperty("isCorrect", out var correctEl) && correctEl.GetBoolean();
                    var optOrderIndex = optionEl.TryGetProperty("orderIndex", out var optOrderEl)
                        ? optOrderEl.GetInt32()
                        : options.Count;
                    var imageUrl = optionEl.TryGetProperty("imageUrl", out var imageEl) ? imageEl.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(optContent))
                    {
                        options.Add(new ExamOptionDraftDto(optContent, isCorrect, optOrderIndex, imageUrl));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(content) && options.Count >= 2)
            {
                results.Add(new ExamQuestionDraftDto(content, explanation, questionType, difficultyLevel, score, orderIndex, options));
            }
        }

        return results;
    }

    private static List<ExamQuestionDraftDto> NormalizeQuestions(List<ExamQuestionDraftDto> questions)
    {
        var normalized = questions
            .OrderBy(q => q.OrderIndex)
            .Select((q, index) =>
            {
                var options = q.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select((o, optIndex) => o with { OrderIndex = optIndex })
                    .ToList();

                if (!options.Any(o => o.IsCorrect))
                {
                    options[0] = options[0] with { IsCorrect = true };
                }

                return q with
                {
                    OrderIndex = index + 1,
                    Score = q.Score <= 0 ? 1 : q.Score,
                    Options = options
                };
            })
            .ToList();

        return normalized;
    }

    private async Task<List<ExamQuestionDraftDto>> ParseExcelAsync(IFormFile file)
    {
        var results = new List<ExamQuestionDraftDto>();

        await using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.Row(1);
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastColumn == 0)
        {
            return results;
        }

        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= lastColumn; col++)
        {
            var header = NormalizeHeader(headerRow.Cell(col).GetString());
            if (!string.IsNullOrWhiteSpace(header))
            {
                headerMap[header] = col;
            }
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= lastRow; row++)
        {
            var question = ParseQuestionRow(
                key => GetCellValue(worksheet, row, headerMap, key),
                row - 1);

            if (question != null)
            {
                results.Add(question);
            }
        }

        return NormalizeQuestions(results);
    }

    private async Task<List<ExamQuestionDraftDto>> ParseCsvAsync(IFormFile file)
    {
        var results = new List<ExamQuestionDraftDto>();

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return results;
        }

        var headers = ParseCsvLine(headerLine);
        var headerMap = headers
            .Select((header, index) => new { Header = NormalizeHeader(header), Index = index })
            .Where(x => !string.IsNullOrWhiteSpace(x.Header))
            .GroupBy(x => x.Header)
            .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);

        var lineNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseCsvLine(line);

            var question = ParseQuestionRow(
                key => GetCsvValue(values, headerMap, key),
                lineNumber - 1);

            if (question != null)
            {
                results.Add(question);
            }
        }

        return NormalizeQuestions(results);
    }

    private static string NormalizeHeader(string input)
        => input.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");

    private static string GetCellValue(IXLWorksheet worksheet, int row, Dictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var col))
        {
            return string.Empty;
        }

        return worksheet.Cell(row, col).GetString().Trim();
    }

    private static string GetCsvValue(List<string> values, Dictionary<string, int> headerMap, string key)
    {
        if (!headerMap.TryGetValue(key, out var index) || index >= values.Count)
        {
            return string.Empty;
        }

        return values[index].Trim();
    }

    private static ExamQuestionDraftDto? ParseQuestionRow(Func<string, string> valueResolver, int fallbackOrderIndex)
    {
        var content = valueResolver("questioncontent");
        if (string.IsNullOrWhiteSpace(content))
        {
            content = valueResolver("content");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            content = valueResolver("cauhoi");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var optionA = valueResolver("optiona");
        var optionB = valueResolver("optionb");
        var optionC = valueResolver("optionc");
        var optionD = valueResolver("optiond");

        var optionsRaw = new[] { optionA, optionB, optionC, optionD };
        var validOptions = optionsRaw
            .Select((text, idx) => new { Text = text, Index = idx })
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .ToList();

        if (validOptions.Count < 2)
        {
            return null;
        }

        var correctRaw = valueResolver("correctoption");
        if (string.IsNullOrWhiteSpace(correctRaw))
        {
            correctRaw = valueResolver("correct");
        }

        if (string.IsNullOrWhiteSpace(correctRaw))
        {
            correctRaw = valueResolver("dapan");
        }

        var correctIndexes = ParseCorrectIndexes(correctRaw, optionsRaw);

        var options = validOptions
            .Select((opt, idx) => new ExamOptionDraftDto(
                opt.Text,
                correctIndexes.Contains(opt.Index),
                idx,
                null))
            .ToList();

        if (!options.Any(o => o.IsCorrect))
        {
            options[0] = options[0] with { IsCorrect = true };
        }

        var explanation = valueResolver("explanation");
        if (string.IsNullOrWhiteSpace(explanation))
        {
            explanation = valueResolver("giaithich");
        }

        var difficultyLevel = ParseByte(valueResolver("difficultylevel"), 1);
        if (difficultyLevel == 0)
        {
            difficultyLevel = ParseByte(valueResolver("mucdo"), 1);
        }

        var score = ParseDecimal(valueResolver("score"), 1m);
        if (score <= 0)
        {
            score = ParseDecimal(valueResolver("diem"), 1m);
        }

        var questionType = ParseByte(valueResolver("questiontype"), 0);
        if (questionType == 0)
        {
            questionType = ParseByte(valueResolver("loaicauhoi"), 0);
        }

        var orderIndex = ParseInt(valueResolver("orderindex"), fallbackOrderIndex);

        return new ExamQuestionDraftDto(
            content,
            string.IsNullOrWhiteSpace(explanation) ? null : explanation,
            questionType,
            difficultyLevel,
            score,
            orderIndex,
            options);
    }

    private static HashSet<int> ParseCorrectIndexes(string correctRaw, string[] optionsRaw)
    {
        var result = new HashSet<int>();

        if (string.IsNullOrWhiteSpace(correctRaw))
        {
            return result;
        }

        var parts = correctRaw.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            parts = [correctRaw.Trim()];
        }

        foreach (var part in parts)
        {
            var token = part.Trim().ToUpperInvariant();

            if (token is "A" or "B" or "C" or "D")
            {
                result.Add(token[0] - 'A');
                continue;
            }

            if (int.TryParse(token, out var number))
            {
                var index = number - 1;
                if (index >= 0 && index < optionsRaw.Length)
                {
                    result.Add(index);
                }

                continue;
            }

            for (var i = 0; i < optionsRaw.Length; i++)
            {
                if (string.Equals(optionsRaw[i], part, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(i);
                }
            }
        }

        return result;
    }

    private static byte ParseByte(string raw, byte defaultValue)
        => byte.TryParse(raw, out var value) ? value : defaultValue;

    private static int ParseInt(string raw, int defaultValue)
        => int.TryParse(raw, out var value) ? value : defaultValue;

    private static decimal ParseDecimal(string raw, decimal defaultValue)
        => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }

    private async Task<Guid> SaveDraftAsExamAsync(int subjectId, Guid createdByUserId, ExamDraftDto draft)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;

            var exam = new Exam
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                CreatedByUserId = createdByUserId,
                Title = draft.Title,
                Description = draft.Description,
                Instructions = draft.Instructions,
                Duration = draft.Duration,
                TotalQuestions = draft.Questions.Count,
                PassScore = draft.PassScore,
                MaxAttempts = draft.MaxAttempts,
                ShuffleQuestions = draft.ShuffleQuestions,
                ShuffleAnswers = draft.ShuffleAnswers,
                ShowResultAfter = draft.ShowResultAfter,
                ShowCorrectAnswer = draft.ShowCorrectAnswer,
                Status = draft.Status,
                StartDate = draft.StartDate,
                EndDate = draft.EndDate,
                AccessCode = draft.AccessCode,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Exams.Add(exam);

            foreach (var questionDraft in draft.Questions.OrderBy(q => q.OrderIndex))
            {
                var question = new Question
                {
                    Id = Guid.NewGuid(),
                    SubjectId = subjectId,
                    CreatedByUserId = createdByUserId,
                    Content = questionDraft.Content,
                    QuestionType = questionDraft.QuestionType,
                    DifficultyLevel = questionDraft.DifficultyLevel,
                    Explanation = questionDraft.Explanation,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Questions.Add(question);

                var normalizedOptions = questionDraft.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select((o, idx) => o with { OrderIndex = idx })
                    .ToList();

                if (!normalizedOptions.Any(o => o.IsCorrect))
                {
                    normalizedOptions[0] = normalizedOptions[0] with { IsCorrect = true };
                }

                foreach (var option in normalizedOptions)
                {
                    _context.Answers.Add(new Answer
                    {
                        QuestionId = question.Id,
                        Content = option.Content,
                        ImageUrl = option.ImageUrl,
                        IsCorrect = option.IsCorrect,
                        OrderIndex = option.OrderIndex
                    });
                }

                _context.ExamQuestions.Add(new ExamQuestion
                {
                    ExamId = exam.Id,
                    QuestionId = question.Id,
                    OrderIndex = questionDraft.OrderIndex,
                    Score = questionDraft.Score <= 0 ? 1 : questionDraft.Score
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return exam.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
