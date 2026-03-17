using System;
using System.Collections.Generic;

namespace ExamSystem.DTOs;

public record GroupDto(
    int Id,
    string Name,
    string Code,
    string? Description,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    List<GroupMemberDto>? Members = null);

public record GroupCreateDto(
    string Name,
    string Code,
    string? Description,
    Guid CreatedByUserId);

public record GroupUpdateDto(
    string Name,
    string Code,
    string? Description);

public record GroupFilterDto(
    string? Keyword,
    int? Page,
    int? PageSize);

public record AddGroupMemberDto(Guid UserId);
