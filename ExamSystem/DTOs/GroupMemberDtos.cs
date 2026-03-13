using System;

namespace ExamSystem.DTOs;

public record GroupMemberDto(
    int GroupId,
    Guid UserId,
    DateTime JoinedAt);

public record GroupMemberCreateDto(
    int GroupId,
    Guid UserId);
