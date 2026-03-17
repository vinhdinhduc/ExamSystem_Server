using ExamSystem.Common;
using ExamSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(ExamSystemDbContext context, AdminSettings adminSettings)
    {
        if (string.IsNullOrWhiteSpace(adminSettings.Username) ||
            string.IsNullOrWhiteSpace(adminSettings.Email) ||
            string.IsNullOrWhiteSpace(adminSettings.Password))
            throw new InvalidOperationException(
                "AdminSettings chưa được cấu hình. Vui lòng điền đầy đủ Username, Email, Password trong appsettings.json.");

        var now = DateTime.UtcNow;

        // ── 1. Permissions ──────────────────────────────────────────────────────
        var permissionDefinitions = new (string Code, string Description)[]
        {
            ("USER_VIEW", "Xem danh sách người dùng"),
            ("USER_CREATE", "Tạo người dùng mới"),
            ("USER_UPDATE", "Cập nhật thông tin người dùng"),
            ("USER_DELETE", "Xóa người dùng"),

            ("ROLE_VIEW", "Xem danh sách vai trò"),
            ("ROLE_CREATE", "Tạo vai trò mới"),
            ("ROLE_UPDATE", "Cập nhật vai trò"),
            ("ROLE_DELETE", "Xóa vai trò"),
            ("ROLE_ASSIGN_PERMISSION", "Gán quyền cho vai trò"),

            ("PERMISSION_VIEW", "Xem danh sách quyền"),
            ("PERMISSION_CREATE", "Tạo quyền mới"),
            ("PERMISSION_UPDATE", "Cập nhật quyền"),
            ("PERMISSION_DELETE", "Xóa quyền"),

            ("SUBJECT_VIEW", "Xem danh sách môn học"),
            ("SUBJECT_CREATE", "Tạo môn học mới"),
            ("SUBJECT_UPDATE", "Cập nhật môn học"),
            ("SUBJECT_DELETE", "Xóa môn học"),

            ("QUESTION_VIEW", "Xem ngân hàng câu hỏi"),
            ("QUESTION_CREATE", "Tạo câu hỏi mới"),
            ("QUESTION_UPDATE", "Cập nhật câu hỏi"),
            ("QUESTION_DELETE", "Xóa câu hỏi"),

            ("EXAM_VIEW", "Xem danh sách đề thi"),
            ("EXAM_CREATE", "Tạo đề thi mới"),
            ("EXAM_UPDATE", "Cập nhật đề thi"),
            ("EXAM_DELETE", "Xóa đề thi"),
            ("EXAM_ASSIGN", "Phân công đề thi"),
            ("EXAM_PUBLISH", "Xuất bản đề thi"),

            ("GROUP_VIEW", "Xem danh sách nhóm"),
            ("GROUP_CREATE", "Tạo nhóm mới"),
            ("GROUP_UPDATE", "Cập nhật nhóm"),
            ("GROUP_DELETE", "Xóa nhóm"),
            ("GROUP_MEMBER_MANAGE", "Quản lý thành viên nhóm"),

            ("EXAM_SESSION_VIEW", "Xem bài làm của học sinh"),
            ("EXAM_SESSION_GRADE", "Chấm điểm bài thi")
        };

        var permissionCodes = permissionDefinitions.Select(x => x.Code).ToList();
        var permissions = await context.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code);

        var missingPermissions = permissionDefinitions
            .Where(x => !permissions.ContainsKey(x.Code))
            .Select(x => new Permission { Code = x.Code, Description = x.Description })
            .ToList();

        if (missingPermissions.Count > 0)
        {
            await context.Permissions.AddRangeAsync(missingPermissions);
            await context.SaveChangesAsync();
            foreach (var p in missingPermissions)
                permissions[p.Code] = p;
        }

        // ── 2. Roles ────────────────────────────────────────────────────────────
        var roleDefinitions = new (string Name, string Description)[]
        {
            ("Admin",   "Quản trị viên hệ thống - Toàn quyền"),
            ("Teacher", "Giáo viên - Quản lý đề thi và câu hỏi"),
            ("Student", "Học sinh - Làm bài thi")
        };

        var roleNames = roleDefinitions.Select(x => x.Name).ToList();
        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name);

        var missingRoles = roleDefinitions
            .Where(x => !roles.ContainsKey(x.Name))
            .Select(x => new Role { Name = x.Name, Description = x.Description, CreatedAt = now })
            .ToList();

        if (missingRoles.Count > 0)
        {
            await context.Roles.AddRangeAsync(missingRoles);
            await context.SaveChangesAsync();
            foreach (var r in missingRoles)
                roles[r.Name] = r;
        }

        // ── 3. Role-Permission mapping ──────────────────────────────────────────
        var rolePermissionRules = new Dictionary<string, Func<string, bool>>
        {
            ["Admin"]   = _ => true,
            ["Teacher"] = code =>
                code.StartsWith("SUBJECT_") ||
                code.StartsWith("QUESTION_") ||
                code.StartsWith("EXAM_") ||
                code.StartsWith("GROUP_") ||
                code == "USER_VIEW",
            ["Student"] = code => code == "EXAM_VIEW"
        };

        var existingRolePermissionPairs = await context.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();

        var rolePermissionsToAdd = new List<RolePermission>();
        foreach (var roleRule in rolePermissionRules)
        {
            var roleId = roles[roleRule.Key].Id;
            foreach (var permission in permissions.Values.Where(p => roleRule.Value(p.Code)))
            {
                if (!existingRolePermissionPairs.Any(x => x.RoleId == roleId && x.PermissionId == permission.Id))
                {
                    rolePermissionsToAdd.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permission.Id
                    });
                }
            }
        }

        if (rolePermissionsToAdd.Count > 0)
        {
            await context.RolePermissions.AddRangeAsync(rolePermissionsToAdd);
            await context.SaveChangesAsync();
        }

        // ── 4. Admin user (từ AdminSettings) ────────────────────────────────────
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == adminSettings.Username);

        if (adminUser == null)
        {
            var passwordHasher = new PasswordHasher<User>();
            adminUser = new User
            {
                Username        = adminSettings.Username,
                Email           = adminSettings.Email,
                FullName        = adminSettings.FullName,
                IsActive        = true,
                IsEmailVerified = true,
                CreatedAt       = now,
                UpdatedAt       = now
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminSettings.Password);

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            // Cập nhật email/fullName nếu cấu hình thay đổi
            var changed = false;
            if (adminUser.Email != adminSettings.Email)
            {
                adminUser.Email = adminSettings.Email;
                changed = true;
            }
            if (adminUser.FullName != adminSettings.FullName)
            {
                adminUser.FullName = adminSettings.FullName;
                changed = true;
            }
            if (!adminUser.IsEmailVerified || !adminUser.IsActive)
            {
                adminUser.IsEmailVerified = true;
                adminUser.IsActive = true;
                changed = true;
            }
            if (changed)
                await context.SaveChangesAsync();
        }

        // ── 5. Gán role Admin cho admin user ────────────────────────────────────
        var adminRoleId = roles["Admin"].Id;
        var hasAdminRole = await context.UserRoles
            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRoleId);

        if (!hasAdminRole)
        {
            await context.UserRoles.AddAsync(new UserRole
            {
                UserId     = adminUser.Id,
                RoleId     = adminRoleId,
                AssignedAt = now
            });
            await context.SaveChangesAsync();
        }
    }
}
