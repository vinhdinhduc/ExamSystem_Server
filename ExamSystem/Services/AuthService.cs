using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ExamSystem.Common;
using ExamSystem.Data;
using ExamSystem.DTOs;
using ExamSystem.Models;
using ExamSystem.Repositories.Interfaces;
using ExamSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExamSystem.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly ExamSystemDbContext _context;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IMapper mapper,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        ExamSystemDbContext context)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
        _context = context;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' đã được đăng ký");

        // Tự sinh username từ phần trước @ của email nếu không truyền
        var baseUsername = dto.Username?.Trim() is { Length: > 0 } u
            ? u
            : dto.Email.Split('@')[0];

        // Đảm bảo username unique — thêm số ngẫu nhiên nếu trùng
        var username = baseUsername;
        if (await _userRepository.ExistsByUsernameAsync(username))
            username = $"{baseUsername}{Random.Shared.Next(100, 9999)}";

        // Create user with pending email verification
        var user = new User
        {
            Username = username,
            Email = dto.Email,
            FullName = dto.FullName?.Trim() is { Length: > 0 } fn ? fn : username,
            IsActive = false,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        // Save user
        var createdUser = await _userRepository.CreateAsync(user);

        // Create email verification token
        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
        await _context.EmailVerificationTokens.AddAsync(new EmailVerificationToken
        {
            UserId = createdUser.Id,
            Token = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Auto-assign Student role
        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        if (studentRole != null)
        {
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId     = createdUser.Id,
                RoleId     = studentRole.Id,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // Send verification email
        var verificationLink = $"{_emailSettings.FrontendBaseUrl}/verify-email?token={verificationToken}";
        await _emailService.SendEmailVerificationAsync(createdUser.Email, createdUser.FullName, verificationLink);

        // Return empty tokens — user must verify email before login
        return new AuthResponseDto(
            string.Empty,
            string.Empty,
            DateTime.UtcNow,
            ToUserInfoDto(createdUser)
        );
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Tìm user bằng username hoặc email trước để lấy Id
        var foundUser = await _userRepository.GetByUsernameAsync(dto.UsernameOrEmail)
                        ?? await _userRepository.GetByEmailAsync(dto.UsernameOrEmail);

        if (foundUser == null)
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng");

        // Load lại kèm roles để include vào JWT
        var user = await _userRepository.GetByIdWithRolesAsync(foundUser.Id);

        if (user == null)
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng");

        // Check email verified trước — user chưa verify thì IsActive cũng false
        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("EMAIL_NOT_VERIFIED");

        // Check if user is active (bị admin khóa)
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa");

        // Verify password
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng");

        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Save refresh token
        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            ToUserInfoDto(user)
        );
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại");

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null)
            throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại");

        if (storedToken.IsRevoked)
            throw new UnauthorizedAccessException("Phiên đăng nhập đã bị thu hồi");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại");

        var userId = storedToken.UserId;

        // Load user kèm roles để include trong JWT
        var user = await _userRepository.GetByIdWithRolesAsync(userId);

        if (user == null)
            throw new UnauthorizedAccessException("Tài khoản không tồn tại");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa");

        // Generate new tokens
        var accessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Revoke old refresh token
        storedToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(storedToken);

        // Save new refresh token
        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        return new AuthResponseDto(
            accessToken,
            newRefreshToken,
            expiresAt,
            ToUserInfoDto(user)
        );
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken != null)
        {
            // Xóa toàn bộ refresh token của user khỏi DB
            await _refreshTokenRepository.DeleteAllUserTokensAsync(storedToken.UserId);
        }
    }

    public async Task VerifyEmailAsync(string token)
    {
        var record = await _context.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (record == null)
            throw new InvalidOperationException("Link xác thực không hợp lệ");

        if (record.IsUsed)
            throw new InvalidOperationException("Link xác thực đã được sử dụng");

        if (record.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Link xác thực đã hết hạn");

        record.IsUsed = true;
        record.User.IsEmailVerified = true;
        record.User.IsActive = true;
        await _context.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        // Luôn trả success để tránh email enumeration attack
        if (user == null || !user.IsEmailVerified) return;

        // Vô hiệu hóa các OTP cũ chưa dùng
        var oldTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync();
        oldTokens.ForEach(t => t.IsUsed = true);

        // Tạo OTP 6 số
        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        await _context.PasswordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _emailService.SendPasswordResetOtpAsync(user.Email, user.FullName, otp);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new InvalidOperationException("Mật khẩu xác nhận không khớp");

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            throw new InvalidOperationException("Email không tồn tại");

        var record = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Otp == dto.Otp && !t.IsUsed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (record == null)
            throw new InvalidOperationException("Mã OTP không hợp lệ");

        if (record.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Mã OTP đã hết hạn");

        record.IsUsed = true;
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        await _context.SaveChangesAsync();
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("fullName", user.FullName),
            new Claim("isActive", user.IsActive.ToString())
        };

        // Thêm role claims từ UserRoles navigation property
        foreach (var userRole in user.UserRoles)
        {
            if (userRole.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                claims.Add(new Claim("roles", userRole.Role.Name));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static UserInfoDto ToUserInfoDto(User user)
    {
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        return new UserInfoDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.IsActive,
            user.CreatedAt,
            roles
        );
    }
}
