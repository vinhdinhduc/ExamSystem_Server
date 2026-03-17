using System;
using System.Collections.Generic;

namespace ExamSystem.Models;

public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
