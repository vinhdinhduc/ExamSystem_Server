using System;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSystem.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly ILogger<AuthController> _logger;
    private const string RefreshTokenCookieName = "refresh_token";

    public AuthController(
        IAuthService authService,
        IValidator<RegisterDto> registerValidator,
        ILogger<AuthController> logger,
        IValidator<LoginDto> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var validationResult = await _registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        var response = await _authService.RegisterAsync(dto);

        return CreatedAtAction(
            nameof(Register),
            null,
            ApiResponse<object>.Success(
                new { email = response.User.Email },
                "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.",
                201
            )
        );
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var validationResult = await _loginValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Dữ liệu không hợp lệ",
                400
            ));
        }

        AuthResponseDto response;
        try
        {
            response = await _authService.LoginAsync(dto);
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "EMAIL_NOT_VERIFIED")
        {
            return Unauthorized(ApiResponse<object>.Failure(
                new { code = "EMAIL_NOT_VERIFIED" },
                "Email chưa được xác thực. Vui lòng kiểm tra hộp thư và click vào link xác thực.",
                401
            ));
        }

        // Set refresh token cookie
        SetRefreshTokenCookie(response.RefreshToken);

        // Return only access token in response
        var result = new
        {
            access_token = response.AccessToken,
            refresh_token = response.RefreshToken, // Optionally include refresh token in response body
            expires_in = 900, // 15 minutes in seconds
            user = response.User
        };

        return Ok(ApiResponse<object>.Success(
            result,
            "Đăng nhập thành công"
        ));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        // Get refresh token from cookie
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<object>.Failure(
                new UnauthorizedError { Reason = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại" },
                "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại",
                401
            ));
        }

        var response = await _authService.RefreshTokenAsync(refreshToken);

        // Set new refresh token cookie
        SetRefreshTokenCookie(response.RefreshToken);

        // Return only access token in response
        var result = new
        {
            access_token = response.AccessToken,
            expires_in = 900, 
            user = response.User
        };

        return Ok(ApiResponse<object>.Success(
            result,
            "Làm mới token thành công"
        ));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Get refresh token from cookie
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }

        // Delete refresh token cookie
        Response.Cookies.Delete(RefreshTokenCookieName, new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });

        return Ok(ApiResponse<object>.Success(
            null,
            "Đăng xuất thành công"
        ));
    }

    [AllowAnonymous]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(ApiResponse<object>.Failure(null, "Token không hợp lệ", 400));

        await _authService.VerifyEmailAsync(token);

        return Ok(ApiResponse<object>.Success(null, "Xác thực email thành công. Bạn có thể đăng nhập ngay bây giờ."));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(ApiResponse<object>.Failure(null, "Email không hợp lệ", 400));

        await _authService.ForgotPasswordAsync(dto.Email);

        return Ok(ApiResponse<object>.Success(null, "Nếu email tồn tại, mã OTP đã được gửi. Vui lòng kiểm tra hộp thư."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);

        return Ok(ApiResponse<object>.Success(null, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập."));
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = true, 
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/v1/auth" 
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }
}
