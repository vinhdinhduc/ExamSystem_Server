using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using ExamSystem.Common;
using ExamSystem.DTOs;
using ExamSystem.Services.Interfaces;
using FluentValidation;
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
    private readonly IValidator<RefreshTokenRequestDto> _refreshValidator;
    private readonly IValidator<LogoutDto> _logoutValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<RefreshTokenRequestDto> refreshValidator,
        IValidator<LogoutDto> logoutValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logoutValidator = logoutValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var validationResult = await _registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        var response = await _authService.RegisterAsync(dto);
        return CreatedAtAction(
            nameof(Register),
            null,
            ApiResponse<AuthResponseDto>.Success(response, "User registered successfully", 201)
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var validationResult = await _loginValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        var response = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Success(
            response,
            "Login successful"
        ));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var validationResult = await _refreshValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        var response = await _authService.RefreshTokenAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Success(
            response,
            "Token refreshed successfully"
        ));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
    {
        var validationResult = await _logoutValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Failure(
                new ValidationError { Details = validationResult.Errors.Select(e => new ValidationDetail { Field = e.PropertyName, Message = e.ErrorMessage }).ToList() },
                "Validation failed",
                400
            ));
        }

        await _authService.LogoutAsync(dto);
        return Ok(ApiResponse<object>.Success(
            null,
            "Logout successful"
        ));
    }
}
