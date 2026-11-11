namespace VibeMovies.Domain.Models;

public class CinemaRadarResponseDto
{
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public string ScraperSource { get; set; } = string.Empty;

    /// <summary>true quando os horários NÃO vieram de uma fonte real (grade ilustrativa gerada localmente).</summary>
    public bool IsSimulatedSchedule { get; set; }

    /// <summary>Aviso em pt-BR para exibir ao usuário (ex.: horários ilustrativos).</summary>
    public string Notice { get; set; } = string.Empty;
    public List<NowPlayingMovieWithShowtimesDto> MoviesInTheaters { get; set; } = new();
}

public class NowPlayingMovieWithShowtimesDto
{
    public MovieCardDto Movie { get; set; } = new();
    public string AgeRating { get; set; } = "Classificação não informada";
    public List<CinemaVenueShowtimesDto> Venues { get; set; } = new();
}

public class CinemaVenueShowtimesDto
{
    public string CinemaChain { get; set; } = string.Empty; // Ex: GNC Cinemas, Cinemark, Cinépolis
    public string CinemaName { get; set; } = string.Empty;  // Ex: GNC Garten Shopping Joinville
    public string Address { get; set; } = string.Empty;
    public List<CinemaSessionDto> Sessions { get; set; } = new();
}

public class CinemaSessionDto
{
    public string Time { get; set; } = string.Empty;        // Ex: "19:30", "21:45"
    public string RoomType { get; set; } = string.Empty;    // Ex: "IMAX", "VIP", "Sala 3 - 2D", "XD"
    public string AudioType { get; set; } = string.Empty;   // Ex: "Legendado", "Dublado"
    public string TicketUrl { get; set; } = string.Empty;
}
