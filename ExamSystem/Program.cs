using System.Text;
using Asp.Versioning;
using ExamSystem.Authorization;
using ExamSystem.Common;
using ExamSystem.Data;
using ExamSystem.Middleware;
using ExamSystem.Models;
using ExamSystem.Repositories;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services;
using ExamSystem.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExamSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Gemini Settings
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));

// Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Admin Settings
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

// Repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IExamSessionRepository, ExamSessionRepository>();

// Services
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IExamSessionService, ExamSessionService>();
builder.Services.AddScoped<IExamAuthoringService, ExamAuthoringService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();

// Password Hasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddHttpClient();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Cấu hình CORS để cho phép frontend truy cập API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings!.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                statusCode = 401,
                error = new
                {
                    code = "UNAUTHORIZED",
                    reason = "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn"
                },
                message = "Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn",
                data = (object?)null
            });

            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                statusCode = 403,
                error = new
                {
                    code = "FORBIDDEN",
                    reason = "Bạn không có quyền truy cập tài nguyên này"
                },
                message = "Bạn không có quyền truy cập tài nguyên này",
                data = (object?)null
            });

            return context.Response.WriteAsync(result);
        }
    };
});

// Permission-based Authorization
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Permission:USER_VIEW",             p => p.AddRequirements(new PermissionRequirement(Permissions.UserView)))
    .AddPolicy("Permission:USER_CREATE",           p => p.AddRequirements(new PermissionRequirement(Permissions.UserCreate)))
    .AddPolicy("Permission:USER_UPDATE",           p => p.AddRequirements(new PermissionRequirement(Permissions.UserUpdate)))
    .AddPolicy("Permission:USER_DELETE",           p => p.AddRequirements(new PermissionRequirement(Permissions.UserDelete)))
    .AddPolicy("Permission:ROLE_VIEW",             p => p.AddRequirements(new PermissionRequirement(Permissions.RoleView)))
    .AddPolicy("Permission:ROLE_CREATE",           p => p.AddRequirements(new PermissionRequirement(Permissions.RoleCreate)))
    .AddPolicy("Permission:ROLE_UPDATE",           p => p.AddRequirements(new PermissionRequirement(Permissions.RoleUpdate)))
    .AddPolicy("Permission:ROLE_DELETE",           p => p.AddRequirements(new PermissionRequirement(Permissions.RoleDelete)))
    .AddPolicy("Permission:ROLE_ASSIGN_PERMISSION",p => p.AddRequirements(new PermissionRequirement(Permissions.RoleAssignPermission)))
    .AddPolicy("Permission:PERMISSION_VIEW",       p => p.AddRequirements(new PermissionRequirement(Permissions.PermissionView)))
    .AddPolicy("Permission:PERMISSION_CREATE",     p => p.AddRequirements(new PermissionRequirement(Permissions.PermissionCreate)))
    .AddPolicy("Permission:PERMISSION_UPDATE",     p => p.AddRequirements(new PermissionRequirement(Permissions.PermissionUpdate)))
    .AddPolicy("Permission:PERMISSION_DELETE",     p => p.AddRequirements(new PermissionRequirement(Permissions.PermissionDelete)))
    .AddPolicy("Permission:SUBJECT_VIEW",          p => p.AddRequirements(new PermissionRequirement(Permissions.SubjectView)))
    .AddPolicy("Permission:SUBJECT_CREATE",        p => p.AddRequirements(new PermissionRequirement(Permissions.SubjectCreate)))
    .AddPolicy("Permission:SUBJECT_UPDATE",        p => p.AddRequirements(new PermissionRequirement(Permissions.SubjectUpdate)))
    .AddPolicy("Permission:SUBJECT_DELETE",        p => p.AddRequirements(new PermissionRequirement(Permissions.SubjectDelete)))
    .AddPolicy("Permission:QUESTION_VIEW",         p => p.AddRequirements(new PermissionRequirement(Permissions.QuestionView)))
    .AddPolicy("Permission:QUESTION_CREATE",       p => p.AddRequirements(new PermissionRequirement(Permissions.QuestionCreate)))
    .AddPolicy("Permission:QUESTION_UPDATE",       p => p.AddRequirements(new PermissionRequirement(Permissions.QuestionUpdate)))
    .AddPolicy("Permission:QUESTION_DELETE",       p => p.AddRequirements(new PermissionRequirement(Permissions.QuestionDelete)))
    .AddPolicy("Permission:EXAM_VIEW",             p => p.AddRequirements(new PermissionRequirement(Permissions.ExamView)))
    .AddPolicy("Permission:EXAM_CREATE",           p => p.AddRequirements(new PermissionRequirement(Permissions.ExamCreate)))
    .AddPolicy("Permission:EXAM_UPDATE",           p => p.AddRequirements(new PermissionRequirement(Permissions.ExamUpdate)))
    .AddPolicy("Permission:EXAM_DELETE",           p => p.AddRequirements(new PermissionRequirement(Permissions.ExamDelete)))
    .AddPolicy("Permission:EXAM_ASSIGN",           p => p.AddRequirements(new PermissionRequirement(Permissions.ExamAssign)))
    .AddPolicy("Permission:EXAM_PUBLISH",          p => p.AddRequirements(new PermissionRequirement(Permissions.ExamPublish)))
    .AddPolicy("Permission:GROUP_VIEW",            p => p.AddRequirements(new PermissionRequirement(Permissions.GroupView)))
    .AddPolicy("Permission:GROUP_CREATE",          p => p.AddRequirements(new PermissionRequirement(Permissions.GroupCreate)))
    .AddPolicy("Permission:GROUP_UPDATE",          p => p.AddRequirements(new PermissionRequirement(Permissions.GroupUpdate)))
    .AddPolicy("Permission:GROUP_DELETE",          p => p.AddRequirements(new PermissionRequirement(Permissions.GroupDelete)))
    .AddPolicy("Permission:GROUP_MEMBER_MANAGE",   p => p.AddRequirements(new PermissionRequirement(Permissions.GroupMemberManage)))
    .AddPolicy("Permission:EXAM_SESSION_VIEW",     p => p.AddRequirements(new PermissionRequirement(Permissions.ExamSessionView)))
    .AddPolicy("Permission:EXAM_SESSION_GRADE",    p => p.AddRequirements(new PermissionRequirement(Permissions.ExamSessionGrade)));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader()
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Cấu hình CORS để cho phép frontend truy cập API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://localhost:5173",
                "https://localhost:3000"
              )
              .AllowCredentials() 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ExamSystemDbContext>();
        var adminSettings = services.GetRequiredService<IConfiguration>()
            .GetSection("AdminSettings").Get<AdminSettings>() ?? new AdminSettings();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedDataAsync(context, adminSettings);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseCors("AllowFrontend");

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseStaticFiles(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
