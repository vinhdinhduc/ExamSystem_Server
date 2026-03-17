using ExamSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(ExamSystemDbContext context)
    {
        var now = DateTime.UtcNow;

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

            foreach (var permission in missingPermissions)
            {
                permissions[permission.Code] = permission;
            }
        }

        var roleDefinitions = new (string Name, string Description)[]
        {
            ("Admin", "Quản trị viên hệ thống - Toàn quyền"),
            ("Teacher", "Giáo viên - Quản lý đề thi và câu hỏi"),
            ("Student", "Học sinh - Làm bài thi")
        };

        var roleNames = roleDefinitions.Select(x => x.Name).ToList();
        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name);

        var missingRoles = roleDefinitions
            .Where(x => !roles.ContainsKey(x.Name))
            .Select(x => new Role
            {
                Name = x.Name,
                Description = x.Description,
                CreatedAt = now
            })
            .ToList();

        if (missingRoles.Count > 0)
        {
            await context.Roles.AddRangeAsync(missingRoles);
            await context.SaveChangesAsync();

            foreach (var role in missingRoles)
            {
                roles[role.Name] = role;
            }
        }

        var rolePermissionRules = new Dictionary<string, Func<string, bool>>
        {
            ["Admin"] = _ => true,
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
                var exists = existingRolePermissionPairs.Any(x =>
                    x.RoleId == roleId &&
                    x.PermissionId == permission.Id);

                if (!exists)
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

        var userDefinitions = new (string Username, string Email, string FullName)[]
        {
            ("admin", "admin@examsystem.local", "System Admin"),
            ("teacher1", "teacher1@examsystem.local", "Teacher One"),
            ("student1", "student1@examsystem.local", "Student One"),
            ("student2", "student2@examsystem.local", "Student Two")
        };

        var usernames = userDefinitions.Select(x => x.Username).ToList();
        var users = await context.Users
            .Where(u => usernames.Contains(u.Username))
            .ToDictionaryAsync(u => u.Username);

        if (users.Count < userDefinitions.Length)
        {
            var passwordHasher = new PasswordHasher<User>();
            var usersToAdd = new List<User>();

            foreach (var userDef in userDefinitions.Where(x => !users.ContainsKey(x.Username)))
            {
                var user = new User
                {
                    Username = userDef.Username,
                    Email = userDef.Email,
                    FullName = userDef.FullName,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                user.PasswordHash = passwordHasher.HashPassword(user, "Password@123");
                usersToAdd.Add(user);
            }

            await context.Users.AddRangeAsync(usersToAdd);
            await context.SaveChangesAsync();

            foreach (var user in usersToAdd)
            {
                users[user.Username] = user;
            }
        }

        var userRolePairs = new (Guid UserId, Guid RoleId)[]
        {
            (users["admin"].Id, roles["Admin"].Id),
            (users["teacher1"].Id, roles["Teacher"].Id),
            (users["student1"].Id, roles["Student"].Id),
            (users["student2"].Id, roles["Student"].Id)
        };

        var existingUserRoles = await context.UserRoles
            .Select(x => new { x.UserId, x.RoleId })
            .ToListAsync();

        var userRolesToAdd = userRolePairs
            .Where(pair => !existingUserRoles.Any(x => x.UserId == pair.UserId && x.RoleId == pair.RoleId))
            .Select(pair => new UserRole
            {
                UserId = pair.UserId,
                RoleId = pair.RoleId,
                AssignedAt = now
            })
            .ToList();

        if (userRolesToAdd.Count > 0)
        {
            await context.UserRoles.AddRangeAsync(userRolesToAdd);
            await context.SaveChangesAsync();
        }

        var student1 = users["student1"];
        var hasRefreshToken = await context.RefreshTokens.AnyAsync(rt => rt.UserId == student1.Id);
        if (!hasRefreshToken)
        {
            await context.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = student1.Id,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = now.AddDays(7),
                IsRevoked = false,
                DeviceInfo = "Seeded Device",
                IpAddress = "127.0.0.1",
                CreatedAt = now
            });
            await context.SaveChangesAsync();
        }

        var subjectDefinitions = new (string Code, string Name, string Description)[]
        {
            ("MATH101", "Toán học cơ bản", "Môn toán nền tảng"),
            ("PHYS101", "Vật lý cơ bản", "Môn vật lý nền tảng")
        };

        var subjectCodes = subjectDefinitions.Select(x => x.Code).ToList();
        var subjects = await context.Subjects
            .Where(s => subjectCodes.Contains(s.Code))
            .ToDictionaryAsync(s => s.Code);

        var subjectsToAdd = subjectDefinitions
            .Where(x => !subjects.ContainsKey(x.Code))
            .Select(x => new Subject
            {
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsActive = true,
                CreatedAt = now
            })
            .ToList();

        if (subjectsToAdd.Count > 0)
        {
            await context.Subjects.AddRangeAsync(subjectsToAdd);
            await context.SaveChangesAsync();

            foreach (var subject in subjectsToAdd)
            {
                subjects[subject.Code] = subject;
            }
        }

        var teacher = users["teacher1"];
        var seededQuestionContents = new[]
        {
            "2 + 2 bằng bao nhiêu?",
            "Số nào là số nguyên tố nhỏ nhất?",
            "Gia tốc rơi tự do gần đúng trên Trái Đất là bao nhiêu m/s²?"
        };

        var existingQuestions = await context.Questions
            .Where(q => seededQuestionContents.Contains(q.Content))
            .ToDictionaryAsync(q => q.Content);

        var questionsToAdd = new List<Question>();

        if (!existingQuestions.ContainsKey("2 + 2 bằng bao nhiêu?"))
        {
            questionsToAdd.Add(new Question
            {
                SubjectId = subjects["MATH101"].Id,
                CreatedByUserId = teacher.Id,
                Content = "2 + 2 bằng bao nhiêu?",
                QuestionType = 1,
                DifficultyLevel = 1,
                Explanation = "Phép cộng cơ bản",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!existingQuestions.ContainsKey("Số nào là số nguyên tố nhỏ nhất?"))
        {
            questionsToAdd.Add(new Question
            {
                SubjectId = subjects["MATH101"].Id,
                CreatedByUserId = teacher.Id,
                Content = "Số nào là số nguyên tố nhỏ nhất?",
                QuestionType = 1,
                DifficultyLevel = 2,
                Explanation = "Số nguyên tố đầu tiên là 2",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!existingQuestions.ContainsKey("Gia tốc rơi tự do gần đúng trên Trái Đất là bao nhiêu m/s²?"))
        {
            questionsToAdd.Add(new Question
            {
                SubjectId = subjects["PHYS101"].Id,
                CreatedByUserId = teacher.Id,
                Content = "Gia tốc rơi tự do gần đúng trên Trái Đất là bao nhiêu m/s²?",
                QuestionType = 1,
                DifficultyLevel = 2,
                Explanation = "Giá trị gần đúng là 9.8 m/s²",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (questionsToAdd.Count > 0)
        {
            await context.Questions.AddRangeAsync(questionsToAdd);
            await context.SaveChangesAsync();

            foreach (var question in questionsToAdd)
            {
                existingQuestions[question.Content] = question;
            }
        }

        var q1 = existingQuestions["2 + 2 bằng bao nhiêu?"];
        var q2 = existingQuestions["Số nào là số nguyên tố nhỏ nhất?"];
        var q3 = existingQuestions["Gia tốc rơi tự do gần đúng trên Trái Đất là bao nhiêu m/s²?"];

        if (!await context.Answers.AnyAsync(a => a.QuestionId == q1.Id))
        {
            await context.Answers.AddRangeAsync(new[]
            {
                new Answer { QuestionId = q1.Id, Content = "3", IsCorrect = false, OrderIndex = 1 },
                new Answer { QuestionId = q1.Id, Content = "4", IsCorrect = true, OrderIndex = 2 },
                new Answer { QuestionId = q1.Id, Content = "5", IsCorrect = false, OrderIndex = 3 },
                new Answer { QuestionId = q1.Id, Content = "6", IsCorrect = false, OrderIndex = 4 }
            });
        }

        if (!await context.Answers.AnyAsync(a => a.QuestionId == q2.Id))
        {
            await context.Answers.AddRangeAsync(new[]
            {
                new Answer { QuestionId = q2.Id, Content = "1", IsCorrect = false, OrderIndex = 1 },
                new Answer { QuestionId = q2.Id, Content = "2", IsCorrect = true, OrderIndex = 2 },
                new Answer { QuestionId = q2.Id, Content = "3", IsCorrect = false, OrderIndex = 3 },
                new Answer { QuestionId = q2.Id, Content = "5", IsCorrect = false, OrderIndex = 4 }
            });
        }

        if (!await context.Answers.AnyAsync(a => a.QuestionId == q3.Id))
        {
            await context.Answers.AddRangeAsync(new[]
            {
                new Answer { QuestionId = q3.Id, Content = "8.9", IsCorrect = false, OrderIndex = 1 },
                new Answer { QuestionId = q3.Id, Content = "9.8", IsCorrect = true, OrderIndex = 2 },
                new Answer { QuestionId = q3.Id, Content = "10.8", IsCorrect = false, OrderIndex = 3 },
                new Answer { QuestionId = q3.Id, Content = "11.8", IsCorrect = false, OrderIndex = 4 }
            });
        }

        await context.SaveChangesAsync();

        var examTitle = "Đề thi thử Toán cơ bản";
        var exam = await context.Exams.FirstOrDefaultAsync(e => e.Title == examTitle);
        if (exam == null)
        {
            exam = new Exam
            {
                SubjectId = subjects["MATH101"].Id,
                CreatedByUserId = teacher.Id,
                Title = examTitle,
                Description = "Đề thi mẫu cho học sinh",
                Instructions = "Chọn đáp án đúng nhất cho mỗi câu hỏi",
                Duration = 30,
                TotalQuestions = 2,
                PassScore = 5,
                MaxAttempts = 3,
                ShuffleQuestions = true,
                ShuffleAnswers = false,
                ShowResultAfter = true,
                ShowCorrectAnswer = true,
                Status = 1,
                StartDate = now,
                EndDate = now.AddDays(30),
                AccessCode = "MATHDEMO",
                CreatedAt = now,
                UpdatedAt = now
            };

            await context.Exams.AddAsync(exam);
            await context.SaveChangesAsync();
        }

        if (!await context.ExamQuestions.AnyAsync(eq => eq.ExamId == exam.Id && eq.QuestionId == q1.Id))
        {
            await context.ExamQuestions.AddAsync(new ExamQuestion
            {
                ExamId = exam.Id,
                QuestionId = q1.Id,
                OrderIndex = 1,
                Score = 5
            });
        }

        if (!await context.ExamQuestions.AnyAsync(eq => eq.ExamId == exam.Id && eq.QuestionId == q2.Id))
        {
            await context.ExamQuestions.AddAsync(new ExamQuestion
            {
                ExamId = exam.Id,
                QuestionId = q2.Id,
                OrderIndex = 2,
                Score = 5
            });
        }

        await context.SaveChangesAsync();

        var group = await context.Groups.FirstOrDefaultAsync(g => g.Code == "GRP10A1");
        if (group == null)
        {
            group = new Group
            {
                Name = "Lớp 10A1",
                Code = "GRP10A1",
                Description = "Nhóm học sinh lớp 10A1",
                CreatedByUserId = teacher.Id,
                CreatedAt = now
            };

            await context.Groups.AddAsync(group);
            await context.SaveChangesAsync();
        }

        var student2 = users["student2"];

        if (!await context.GroupMembers.AnyAsync(gm => gm.GroupId == group.Id && gm.UserId == student1.Id))
        {
            await context.GroupMembers.AddAsync(new GroupMember
            {
                GroupId = group.Id,
                UserId = student1.Id,
                JoinedAt = now
            });
        }

        if (!await context.GroupMembers.AnyAsync(gm => gm.GroupId == group.Id && gm.UserId == student2.Id))
        {
            await context.GroupMembers.AddAsync(new GroupMember
            {
                GroupId = group.Id,
                UserId = student2.Id,
                JoinedAt = now
            });
        }

        await context.SaveChangesAsync();

        if (!await context.ExamAssignments.AnyAsync(a => a.ExamId == exam.Id && a.UserId == student1.Id && a.GroupId == null))
        {
            await context.ExamAssignments.AddAsync(new ExamAssignment
            {
                ExamId = exam.Id,
                UserId = student1.Id,
                GroupId = null,
                AssignedAt = now
            });
        }

        if (!await context.ExamAssignments.AnyAsync(a => a.ExamId == exam.Id && a.GroupId == group.Id))
        {
            await context.ExamAssignments.AddAsync(new ExamAssignment
            {
                ExamId = exam.Id,
                UserId = null,
                GroupId = group.Id,
                AssignedAt = now
            });
        }

        await context.SaveChangesAsync();

        var session = await context.ExamSessions
            .FirstOrDefaultAsync(s => s.ExamId == exam.Id && s.UserId == student1.Id && s.AttemptNumber == 1);

        if (session == null)
        {
            session = new ExamSession
            {
                ExamId = exam.Id,
                UserId = student1.Id,
                StartedAt = now.AddMinutes(-25),
                SubmittedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddMinutes(5),
                Status = 2,
                Score = 10,
                IsPassed = true,
                TotalCorrect = 2,
                AttemptNumber = 1,
                IpAddress = "127.0.0.1",
                QuestionOrder = "1,2"
            };

            await context.ExamSessions.AddAsync(session);
            await context.SaveChangesAsync();
        }

        var answersQ1 = await context.Answers.Where(a => a.QuestionId == q1.Id).ToListAsync();
        var answersQ2 = await context.Answers.Where(a => a.QuestionId == q2.Id).ToListAsync();

        var q1CorrectIds = string.Join(',', answersQ1.Where(a => a.IsCorrect).Select(a => a.Id));
        var q2CorrectIds = string.Join(',', answersQ2.Where(a => a.IsCorrect).Select(a => a.Id));

        var sessionAnswerQ1 = await context.SessionAnswers
            .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.QuestionId == q1.Id);
        if (sessionAnswerQ1 == null)
        {
            sessionAnswerQ1 = new SessionAnswer
            {
                SessionId = session.Id,
                QuestionId = q1.Id,
                CorrectAnswerIds = q1CorrectIds,
                IsCorrect = true,
                Score = 5,
                AnsweredAt = now.AddMinutes(-15)
            };

            await context.SessionAnswers.AddAsync(sessionAnswerQ1);
            await context.SaveChangesAsync();
        }

        var sessionAnswerQ2 = await context.SessionAnswers
            .FirstOrDefaultAsync(sa => sa.SessionId == session.Id && sa.QuestionId == q2.Id);
        if (sessionAnswerQ2 == null)
        {
            sessionAnswerQ2 = new SessionAnswer
            {
                SessionId = session.Id,
                QuestionId = q2.Id,
                CorrectAnswerIds = q2CorrectIds,
                IsCorrect = true,
                Score = 5,
                AnsweredAt = now.AddMinutes(-10)
            };

            await context.SessionAnswers.AddAsync(sessionAnswerQ2);
            await context.SaveChangesAsync();
        }

        var selectedQ1 = answersQ1.First(a => a.IsCorrect);
        var selectedQ2 = answersQ2.First(a => a.IsCorrect);

        if (!await context.SessionAnswerDetails.AnyAsync(d => d.SessionAnswerId == sessionAnswerQ1.Id && d.AnswerId == selectedQ1.Id))
        {
            await context.SessionAnswerDetails.AddAsync(new SessionAnswerDetail
            {
                SessionAnswerId = sessionAnswerQ1.Id,
                AnswerId = selectedQ1.Id,
                SelectedAt = now.AddMinutes(-15)
            });
        }

        if (!await context.SessionAnswerDetails.AnyAsync(d => d.SessionAnswerId == sessionAnswerQ2.Id && d.AnswerId == selectedQ2.Id))
        {
            await context.SessionAnswerDetails.AddAsync(new SessionAnswerDetail
            {
                SessionAnswerId = sessionAnswerQ2.Id,
                AnswerId = selectedQ2.Id,
                SelectedAt = now.AddMinutes(-10)
            });
        }

        await context.SaveChangesAsync();
    }
}
