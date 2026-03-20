using System.Text.Json;
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

        // ── 6. Seed sample domain data (idempotent) ─────────────────────────────
        await SeedSampleDomainDataAsync(context, roles, adminUser, now);
    }

    private static async Task SeedSampleDomainDataAsync(
        ExamSystemDbContext context,
        Dictionary<string, Role> roles,
        User adminUser,
        DateTime now)
    {
        if (await context.Subjects.AnyAsync(s => s.Code == "CSDL"))
            return;

        var passwordHasher = new PasswordHasher<User>();

        // Subjects
        var subjects = new List<Subject>
        {
            new() { Name = "Cơ sở dữ liệu", Code = "CSDL", Description = "Kiến thức SQL Server", IsActive = true, CreatedAt = now },
            new() { Name = "Lập trình Web", Code = "LTW", Description = "ASP.NET Core và Web API", IsActive = true, CreatedAt = now },
            new() { Name = "Cấu trúc dữ liệu", Code = "CTDL", Description = "Thuật toán và cấu trúc dữ liệu", IsActive = true, CreatedAt = now }
        };
        await context.Subjects.AddRangeAsync(subjects);
        await context.SaveChangesAsync();

        var csdlSubject = subjects.First(s => s.Code == "CSDL");
        var ltwSubject = subjects.First(s => s.Code == "LTW");

        // Users
        var teacher = new User
        {
            Username = "teacher.demo",
            Email = "teacher.demo@examsystem.local",
            FullName = "Demo Teacher",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        teacher.PasswordHash = passwordHasher.HashPassword(teacher, "Teacher@123");

        var student1 = new User
        {
            Username = "student.one",
            Email = "student.one@examsystem.local",
            FullName = "Student One",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        student1.PasswordHash = passwordHasher.HashPassword(student1, "Student@123");

        var student2 = new User
        {
            Username = "student.two",
            Email = "student.two@examsystem.local",
            FullName = "Student Two",
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        student2.PasswordHash = passwordHasher.HashPassword(student2, "Student@123");

        await context.Users.AddRangeAsync(teacher, student1, student2);
        await context.SaveChangesAsync();

        // User roles
        var userRoles = new List<UserRole>
        {
            new() { UserId = teacher.Id, RoleId = roles["Teacher"].Id, AssignedAt = now },
            new() { UserId = student1.Id, RoleId = roles["Student"].Id, AssignedAt = now },
            new() { UserId = student2.Id, RoleId = roles["Student"].Id, AssignedAt = now }
        };
        await context.UserRoles.AddRangeAsync(userRoles);
        await context.SaveChangesAsync();

        // Questions + Answers
        var q1 = new Question
        {
            SubjectId = csdlSubject.Id,
            CreatedByUserId = teacher.Id,
            Content = "Câu lệnh nào dùng để truy vấn dữ liệu trong SQL Server?",
            QuestionType = 0,
            DifficultyLevel = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Explanation = "SELECT dùng để truy vấn dữ liệu"
        };
        q1.Answers = new List<Answer>
        {
            new() { Content = "SELECT", IsCorrect = true, OrderIndex = 0 },
            new() { Content = "INSERT", IsCorrect = false, OrderIndex = 1 },
            new() { Content = "UPDATE", IsCorrect = false, OrderIndex = 2 },
            new() { Content = "DELETE", IsCorrect = false, OrderIndex = 3 }
        };

        var q2 = new Question
        {
            SubjectId = csdlSubject.Id,
            CreatedByUserId = teacher.Id,
            Content = "Khóa chính (Primary Key) có đặc điểm nào đúng?",
            QuestionType = 0,
            DifficultyLevel = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Explanation = "Primary Key phải duy nhất và không null"
        };
        q2.Answers = new List<Answer>
        {
            new() { Content = "Cho phép trùng giá trị", IsCorrect = false, OrderIndex = 0 },
            new() { Content = "Không được null và duy nhất", IsCorrect = true, OrderIndex = 1 },
            new() { Content = "Luôn là kiểu nvarchar", IsCorrect = false, OrderIndex = 2 },
            new() { Content = "Không tạo index", IsCorrect = false, OrderIndex = 3 }
        };

        var q3 = new Question
        {
            SubjectId = ltwSubject.Id,
            CreatedByUserId = teacher.Id,
            Content = "Trong ASP.NET Core, middleware dùng để làm gì?",
            QuestionType = 0,
            DifficultyLevel = 2,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Explanation = "Middleware xử lý request/response pipeline"
        };
        q3.Answers = new List<Answer>
        {
            new() { Content = "Render giao diện frontend", IsCorrect = false, OrderIndex = 0 },
            new() { Content = "Xử lý pipeline HTTP request/response", IsCorrect = true, OrderIndex = 1 },
            new() { Content = "Thay thế DbContext", IsCorrect = false, OrderIndex = 2 },
            new() { Content = "Chỉ dùng cho logging", IsCorrect = false, OrderIndex = 3 }
        };

        await context.Questions.AddRangeAsync(q1, q2, q3);
        await context.SaveChangesAsync();

        // Exam + ExamQuestions
        var exam = new Exam
        {
            SubjectId = csdlSubject.Id,
            CreatedByUserId = teacher.Id,
            Title = "Đề thi thử SQL Server cơ bản",
            Description = "Đề mẫu cho học sinh thực hành",
            Instructions = "Chọn đáp án đúng nhất cho mỗi câu",
            Duration = 30,
            TotalQuestions = 2,
            PassScore = 50,
            MaxAttempts = 2,
            ShuffleQuestions = true,
            ShuffleAnswers = true,
            ShowResultAfter = true,
            ShowCorrectAnswer = true,
            Status = 1,
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(10),
            AccessCode = "SQL2025",
            CreatedAt = now,
            UpdatedAt = now
        };

        await context.Exams.AddAsync(exam);
        await context.SaveChangesAsync();

        var examQuestions = new List<ExamQuestion>
        {
            new() { ExamId = exam.Id, QuestionId = q1.Id, OrderIndex = 1, Score = 5 },
            new() { ExamId = exam.Id, QuestionId = q2.Id, OrderIndex = 2, Score = 5 }
        };
        await context.ExamQuestions.AddRangeAsync(examQuestions);
        await context.SaveChangesAsync();

        // Group + Members
        var group = new Group
        {
            Name = "Lớp CNTT K20A",
            Code = "CNTT-K20A",
            Description = "Nhóm học sinh demo",
            CreatedByUserId = teacher.Id,
            CreatedAt = now
        };
        await context.Groups.AddAsync(group);
        await context.SaveChangesAsync();

        await context.GroupMembers.AddRangeAsync(
            new GroupMember { GroupId = group.Id, UserId = student1.Id, JoinedAt = now },
            new GroupMember { GroupId = group.Id, UserId = student2.Id, JoinedAt = now });
        await context.SaveChangesAsync();

        // Assignments
        await context.ExamAssignments.AddRangeAsync(
            new ExamAssignment { ExamId = exam.Id, GroupId = group.Id, AssignedAt = now },
            new ExamAssignment { ExamId = exam.Id, UserId = student1.Id, AssignedAt = now });
        await context.SaveChangesAsync();

        // Sample Exam Session for student1
        var sessionStartedAt = now.AddHours(-1);
        var session = new ExamSession
        {
            ExamId = exam.Id,
            UserId = student1.Id,
            StartedAt = sessionStartedAt,
            SubmittedAt = now.AddMinutes(-20),
            ExpiresAt = sessionStartedAt.AddMinutes(exam.Duration),
            Status = 1,
            AttemptNumber = 1,
            QuestionOrder = JsonSerializer.Serialize(new List<Guid> { q1.Id, q2.Id }),
            IpAddress = "127.0.0.1"
        };
        await context.ExamSessions.AddAsync(session);
        await context.SaveChangesAsync();

        var q1CorrectIds = q1.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList();
        var q2CorrectIds = q2.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList();

        var sessionAnswer1 = new SessionAnswer
        {
            SessionId = session.Id,
            QuestionId = q1.Id,
            CorrectAnswerIds = JsonSerializer.Serialize(q1CorrectIds),
            IsCorrect = true,
            Score = 5,
            AnsweredAt = now.AddMinutes(-40)
        };

        var sessionAnswer2 = new SessionAnswer
        {
            SessionId = session.Id,
            QuestionId = q2.Id,
            CorrectAnswerIds = JsonSerializer.Serialize(q2CorrectIds),
            IsCorrect = false,
            Score = 0,
            AnsweredAt = now.AddMinutes(-35)
        };

        await context.SessionAnswers.AddRangeAsync(sessionAnswer1, sessionAnswer2);
        await context.SaveChangesAsync();

        var q1Selected = q1.Answers.First(a => a.IsCorrect);
        var q2Selected = q2.Answers.First(a => !a.IsCorrect);

        await context.SessionAnswerDetails.AddRangeAsync(
            new SessionAnswerDetail { SessionAnswerId = sessionAnswer1.Id, AnswerId = q1Selected.Id, SelectedAt = now.AddMinutes(-40) },
            new SessionAnswerDetail { SessionAnswerId = sessionAnswer2.Id, AnswerId = q2Selected.Id, SelectedAt = now.AddMinutes(-35) });

        session.TotalCorrect = 1;
        session.Score = 50;
        session.IsPassed = true;

        await context.SaveChangesAsync();
    }
}
