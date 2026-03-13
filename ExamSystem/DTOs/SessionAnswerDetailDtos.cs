using System;

namespace ExamSystem.DTOs;

public record SessionAnswerDetailDto(
    int Id,
    int SessionAnswerId,
    int AnswerId,
    DateTime? SelectedAt);

public record SessionAnswerDetailCreateDto(
    int SessionAnswerId,
    int AnswerId,
    DateTime? SelectedAt);
