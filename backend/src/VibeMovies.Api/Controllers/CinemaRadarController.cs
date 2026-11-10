using Microsoft.AspNetCore.Mvc;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

[ApiController]
[Route("api/cinemas")]
public class CinemaRadarController : ControllerBase
{
    private readonly ICinemaRadarService _cinemaRadarService;

    public CinemaRadarController(ICinemaRadarService cinemaRadarService)
    {
        _cinemaRadarService = cinemaRadarService;
    }

    [HttpGet("cities")]
    public ActionResult<IReadOnlyList<string>> GetCities()
    {
        return Ok(_cinemaRadarService.GetSupportedCities());
    }

    [HttpGet("now-playing")]
    public async Task<ActionResult<CinemaRadarResponseDto>> GetNowPlayingByCity(
        [FromQuery] string city = "Joinville/SC",
        CancellationToken ct = default)
    {
        var runtimeApiKey = Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();
        var result = await _cinemaRadarService.GetLocalRadarAsync(city, runtimeApiKey, ct);
        return Ok(result);
    }
}
