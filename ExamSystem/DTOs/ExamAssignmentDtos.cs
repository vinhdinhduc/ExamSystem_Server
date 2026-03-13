using System;

namespace ExamSystem.DTOs;

public record ExamAssignmentDto(
    int Id,
    Guid ExamId,
    Guid? UserId,
    int? GroupId,
    DateTime AssignedAt);

public record ExamAssignmentCreateDto(
    Guid ExamId,
    Guid? UserId,
    int? GroupId);
