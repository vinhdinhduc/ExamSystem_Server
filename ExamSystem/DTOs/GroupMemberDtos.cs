using System;

namespace ExamSystem.DTOs;

public record GroupMemberDto(
    int GroupId,
    Guid UserId,
    DateTime JoinedAt,
    string? FullName = null,
    string? Email = null);

public record GroupMemberCreateDto(
    int GroupId,
    Guid UserId);
