using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VibeMovies.Api.Infrastructure;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;

    public AuthController(IAuthService authService, IUserRepository userRepository)
    {
        _authService = authService;
        _userRepository = userRepository;
    }

    [HttpPost("register")]
    [EnableRateLimiting(VibeMoviesApp.AuthRateLimitPolicy)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return GlobalExceptionHandler.Problem(StatusCodes.Status400BadRequest, "O nome é obrigatório.");

        try
        {
            var result = await _authService.RegisterAsync(dto, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return GlobalExceptionHandler.Problem(StatusCodes.Status409Conflict, ex.Message);
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting(VibeMoviesApp.AuthRateLimitPolicy)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        if (result is null)
            return GlobalExceptionHandler.Problem(StatusCodes.Status401Unauthorized, "Credenciais inválidas. Verifique seu e-mail e senha.");

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Unauthorized();
        return Ok(ToProfile(user));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe([FromBody] UpdateProfileRequestDto dto, CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null) return Unauthorized();

        if (dto.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return GlobalExceptionHandler.Problem(StatusCodes.Status400BadRequest, "O nome não pode ficar vazio.");
            user.Name = dto.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.DefaultCity)) user.DefaultCity = dto.DefaultCity.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PreferredRegion)) user.PreferredRegion = dto.PreferredRegion.Trim().ToUpperInvariant();
        if (dto.MyProviders is not null)
        {
            var providers = dto.MyProviders
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(ProviderCatalog.Canonicalize)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var csv = string.Join(",", providers);
            if (csv.Length > 500)
                return GlobalExceptionHandler.Problem(StatusCodes.Status400BadRequest, "Lista de streamings muito longa.");
            user.MyProvidersCsv = csv;
        }

        var updated = await _userRepository.UpdateAsync(user, ct);
        if (updated is null) return NotFound();
        return Ok(ToProfile(updated));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var userId) ? await _userRepository.GetByIdAsync(userId, ct) : null;
    }

    public static UserProfileDto ToProfile(ApplicationUser user) => new()
    {
        UserId = user.Id,
        Name = user.Name,
        Email = user.Email,
        PreferredRegion = user.PreferredRegion,
        DefaultCity = user.DefaultCity,
        MyProviders = ProviderCatalog.ParseList(user.MyProvidersCsv)
    };
}
