using System;

namespace ExamSystem.DTOs;

public record ExamAssignmentDto(
    int Id,
    Guid ExamId,
    Guid? UserId,
    int? GroupId,
    DateTime AssignedAt);

public record ExamAssignmentCreateDto(
    Guid? UserId,
    int? GroupId);

public record StudentAssignedExamDto(
    Guid ExamId,
    string Title,
    int Duration,
    decimal PassScore,
    DateTime? StartDate,
    DateTime? EndDate,
    byte Status,
    DateTime AssignedAt);
