using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BCrypt.Net;
using StudentApi.Security;

namespace StudentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserStore _users;
    private readonly TokenService _tokens;
    private readonly AuditService _audit;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserStore users,
        TokenService tokens,
        AuditService audit,
        ILogger<AuthController> logger)
    {
        _users = users;
        _tokens = tokens;
        _audit = audit;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("Register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<AuthResponse> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Length > 100
            || string.IsNullOrWhiteSpace(request.Email)
            || request.Email.Length > 200
            || string.IsNullOrWhiteSpace(request.Password)
            || request.Password.Length < 8)
        {
            return BadRequest("Invalid registration data.");
        }

        var user = _users.Register(request.Name, request.Email, request.Password);
        if (user is null)
        {
            return Conflict("An account with this email already exists.");
        }

        _audit.Add(user.Email, "Register", "Auth", "Student account registered");
        _logger.LogInformation("Registered student account {Email}", user.Email);

        return StatusCode(StatusCodes.Status201Created, CreateAuthResponse(user));
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("Login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<AuthResponse> Login(LoginRequest request)
    {
        var user = _users.FindByEmail(request.Email);
        if (user is null
            || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash))
        {
            _audit.Add(request.Email, "FailedLogin", "Auth", "Invalid email or password");
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized("Invalid email or password.");
        }

        _audit.Add(user.Email, "Login", "Auth", "Successful login");
        _logger.LogInformation("Successful login for {Email}", user.Email);
        return Ok(CreateAuthResponse(user));
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("Refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<AuthResponse> Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unauthorized("Invalid refresh token.");
        }

        var oldSession = _users.RotateRefreshSession(request.RefreshToken);
        if (oldSession is null)
        {
            return Unauthorized("Invalid or expired refresh token.");
        }

        var user = _users.FindById(oldSession.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid refresh token.");
        }

        var response = CreateAuthResponse(user);
        _audit.Add(user.Email, "Refresh", "Auth", "Refresh token rotated");
        return Ok(response);
    }

    [Authorize]
    [HttpPost("Logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout(RefreshRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            _users.RevokeRefreshSession(request.RefreshToken);
        }

        var email = User.FindFirstValue(ClaimTypes.Email);
        _audit.Add(email, "Logout", "Auth", "Refresh session revoked");
        return NoContent();
    }

    [Authorize]
    [HttpGet("Me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.FindFirstValue(ClaimTypes.Name),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Role = User.FindFirstValue(ClaimTypes.Role),
            StudentId = User.FindFirstValue("student_id")
        });
    }

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        var access = _tokens.CreateAccessToken(user);
        var refreshToken = _users.CreateRefreshSession(user.Id, TimeSpan.FromDays(7));

        return new AuthResponse
        {
            AccessToken = access.Token,
            RefreshToken = refreshToken,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };
    }
}
