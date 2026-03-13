using ExamSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(ExamSystemDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            new Permission { Code = "USER_VIEW", Description = "Xem danh sách người dùng" },
            new Permission { Code = "USER_CREATE", Description = "Tạo người dùng mới" },
            new Permission { Code = "USER_UPDATE", Description = "Cập nhật thông tin người dùng" },
            new Permission { Code = "USER_DELETE", Description = "Xóa người dùng" },
            
            new Permission { Code = "ROLE_VIEW", Description = "Xem danh sách vai trò" },
            new Permission { Code = "ROLE_CREATE", Description = "Tạo vai trò mới" },
            new Permission { Code = "ROLE_UPDATE", Description = "Cập nhật vai trò" },
            new Permission { Code = "ROLE_DELETE", Description = "Xóa vai trò" },
            new Permission { Code = "ROLE_ASSIGN_PERMISSION", Description = "Gán quyền cho vai trò" },
            
            new Permission { Code = "PERMISSION_VIEW", Description = "Xem danh sách quyền" },
            new Permission { Code = "PERMISSION_CREATE", Description = "Tạo quyền mới" },
            new Permission { Code = "PERMISSION_UPDATE", Description = "Cập nhật quyền" },
            new Permission { Code = "PERMISSION_DELETE", Description = "Xóa quyền" },
            
            new Permission { Code = "SUBJECT_VIEW", Description = "Xem danh sách môn học" },
            new Permission { Code = "SUBJECT_CREATE", Description = "Tạo môn học mới" },
            new Permission { Code = "SUBJECT_UPDATE", Description = "Cập nhật môn học" },
            new Permission { Code = "SUBJECT_DELETE", Description = "Xóa môn học" },
            
            new Permission { Code = "QUESTION_VIEW", Description = "Xem ngân hàng câu hỏi" },
            new Permission { Code = "QUESTION_CREATE", Description = "Tạo câu hỏi mới" },
            new Permission { Code = "QUESTION_UPDATE", Description = "Cập nhật câu hỏi" },
            new Permission { Code = "QUESTION_DELETE", Description = "Xóa câu hỏi" },
            
            new Permission { Code = "EXAM_VIEW", Description = "Xem danh sách đề thi" },
            new Permission { Code = "EXAM_CREATE", Description = "Tạo đề thi mới" },
            new Permission { Code = "EXAM_UPDATE", Description = "Cập nhật đề thi" },
            new Permission { Code = "EXAM_DELETE", Description = "Xóa đề thi" },
            new Permission { Code = "EXAM_ASSIGN", Description = "Phân công đề thi" },
            
            new Permission { Code = "GROUP_VIEW", Description = "Xem danh sách nhóm" },
            new Permission { Code = "GROUP_CREATE", Description = "Tạo nhóm mới" },
            new Permission { Code = "GROUP_UPDATE", Description = "Cập nhật nhóm" },
            new Permission { Code = "GROUP_DELETE", Description = "Xóa nhóm" },
            new Permission { Code = "GROUP_MEMBER_MANAGE", Description = "Quản lý thành viên nhóm" },
            
            new Permission { Code = "EXAM_SESSION_VIEW", Description = "Xem bài làm của học sinh" },
            new Permission { Code = "EXAM_SESSION_GRADE", Description = "Chấm điểm bài thi" },
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        var adminRole = new Role
        {
            Name = "Admin",
            Description = "Quản trị viên hệ thống - Toàn quyền",
            CreatedAt = DateTime.UtcNow
        };

        var teacherRole = new Role
        {
            Name = "Teacher",
            Description = "Giáo viên - Quản lý đề thi và câu hỏi",
            CreatedAt = DateTime.UtcNow
        };

        var studentRole = new Role
        {
            Name = "Student",
            Description = "Học sinh - Làm bài thi",
            CreatedAt = DateTime.UtcNow
        };

        await context.Roles.AddRangeAsync(new[] { adminRole, teacherRole, studentRole });
        await context.SaveChangesAsync();

        var adminPermissions = permissions.Select(p => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = p.Id
        }).ToList();

        var teacherPermissions = permissions
            .Where(p => p.Code.StartsWith("SUBJECT_") ||
                       p.Code.StartsWith("QUESTION_") ||
                       p.Code.StartsWith("EXAM_") ||
                       p.Code.StartsWith("GROUP_") ||
                       p.Code == "USER_VIEW")
            .Select(p => new RolePermission
            {
                RoleId = teacherRole.Id,
                PermissionId = p.Id
            })
            .ToList();

        var studentPermissions = permissions
            .Where(p => p.Code == "EXAM_VIEW")
            .Select(p => new RolePermission
            {
                RoleId = studentRole.Id,
                PermissionId = p.Id
            })
            .ToList();

        await context.RolePermissions.AddRangeAsync(adminPermissions);
        await context.RolePermissions.AddRangeAsync(teacherPermissions);
        await context.RolePermissions.AddRangeAsync(studentPermissions);
        await context.SaveChangesAsync();
    }
}
