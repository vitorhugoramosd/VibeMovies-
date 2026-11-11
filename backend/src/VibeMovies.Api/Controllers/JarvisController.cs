using Microsoft.AspNetCore.Mvc;
using VibeMovies.Api.Infrastructure;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

/// <summary>
/// Integração com o assistente Jarvis. Sem JWT: só aceita requisições da própria máquina (loopback).
/// Usuário = Jarvis:UserEmail (padrão demo@vibemovies.app) ou, na falta dele, o primeiro usuário.
/// </summary>
[ApiController]
[Route("api/jarvis")]
[LoopbackOnly]
public class JarvisController : ControllerBase
{
    private readonly JarvisService _jarvis;
    private readonly IConfiguration _configuration;

    public JarvisController(JarvisService jarvis, IConfiguration configuration)
    {
        _jarvis = jarvis;
        _configuration = configuration;
    }

    private Task<ApplicationUser?> ResolveUserAsync(CancellationToken ct) =>
        _jarvis.ResolveUserAsync(_configuration["Jarvis:UserEmail"] ?? "demo@vibemovies.app", ct);

    [HttpGet("summary")]
    public async Task<ActionResult<JarvisSummaryDto>> Summary([FromQuery] string? city = null, CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(ct);
        if (user is null)
            return GlobalExceptionHandler.Problem(StatusCodes.Status404NotFound, "Nenhum usuário cadastrado no VibeMovies.");
        return Ok(await _jarvis.GetSummaryAsync(user, city, ct));
    }

    [HttpGet("surprise")]
    public async Task<ActionResult<SurprisePickDto>> Surprise([FromQuery] string? vibe = null, [FromQuery] int? maxRuntime = null, CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(ct);
        var pick = await _jarvis.SurpriseAsync(user, vibe, maxRuntime, ct);
        if (pick is null)
            return GlobalExceptionHandler.Problem(StatusCodes.Status404NotFound, "Não encontrei nenhum filme para sugerir agora.");
        return Ok(pick);
    }

    [HttpGet("vibes")]
    public ActionResult<IReadOnlyList<JarvisVibeDto>> Vibes() => Ok(JarvisService.GetVibes());

    [HttpPost("watchlist")]
    public async Task<ActionResult<WatchlistItem>> AddToWatchlist([FromBody] JarvisAddToWatchlistRequestDto dto, CancellationToken ct = default)
    {
        if (dto.TmdbId <= 0)
            return GlobalExceptionHandler.Problem(StatusCodes.Status400BadRequest, "Informe um tmdbId válido.");

        var user = await ResolveUserAsync(ct);
        if (user is null)
            return GlobalExceptionHandler.Problem(StatusCodes.Status404NotFound, "Nenhum usuário cadastrado no VibeMovies.");

        var item = await _jarvis.AddToWatchlistAsync(user, dto, ct); // ArgumentException → 400 pelo handler global
        if (item is null)
            return GlobalExceptionHandler.Problem(StatusCodes.Status404NotFound, $"Filme {dto.TmdbId} não encontrado (sem chave do TMDB só o catálogo local está disponível).");
        return Ok(item);
    }
}
