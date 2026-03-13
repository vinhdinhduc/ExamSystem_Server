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
    private const string RefreshTokenCookieName = "refresh_token";

    public AuthController(
        IAuthService authService,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
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

        // Set refresh token cookie
        SetRefreshTokenCookie(response.RefreshToken);

        // Return only access token in response
        var result = new
        {
            access_token = response.AccessToken,
            expires_in = 900, // 15 minutes in seconds
            user = response.User
        };

        return CreatedAtAction(
            nameof(Register),
            null,
            ApiResponse<object>.Success(result, "Đăng ký thành công", 201)
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

        var response = await _authService.LoginAsync(dto);

        // Set refresh token cookie
        SetRefreshTokenCookie(response.RefreshToken);

        // Return only access token in response
        var result = new
        {
            access_token = response.AccessToken,
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
            expires_in = 900, // 15 minutes in seconds
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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Only over HTTPS
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/v1/auth" // Cookie only sent to auth endpoints
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }
}
