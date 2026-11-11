using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VibeMovies.Domain.Entities;

namespace VibeMovies.Application.Services;

/// <summary>Exportação do Diário de Bordo em CSV (separador ";" + BOM UTF-8, abre direto no Excel pt-BR) ou JSON.</summary>
public static class WatchlistExporter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string ListLabel(WatchlistType type) => type == WatchlistType.Watched ? "Já Vi" : "Quero Ver";

    public static byte[] ToCsv(IEnumerable<WatchlistItem> items)
    {
        var sb = new StringBuilder();
        sb.Append("Lista;Título;Título Original;Ano;Gêneros;Duração (min);Nota TMDB;Minha Nota;Assistido em;Adicionado em;Notas Pessoais;TMDB Id\r\n");

        foreach (var i in items)
        {
            var fields = new[]
            {
                ListLabel(i.ListType),
                i.Title,
                i.OriginalTitle,
                i.ReleaseYear?.ToString(CultureInfo.InvariantCulture) ?? "",
                i.GenresCsv,
                i.RuntimeMinutes?.ToString(CultureInfo.InvariantCulture) ?? "",
                i.TmdbVoteAverage > 0 ? i.TmdbVoteAverage.ToString("0.0", PtBr) : "",
                i.PersonalRating?.ToString(CultureInfo.InvariantCulture) ?? "",
                i.WatchedAt?.ToString("dd/MM/yyyy", PtBr) ?? "",
                i.AddedAt.ToString("dd/MM/yyyy", PtBr),
                i.PersonalNotes ?? "",
                i.TmdbMovieId.ToString(CultureInfo.InvariantCulture)
            };

            sb.Append(string.Join(';', fields.Select(Escape))).Append("\r\n");
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    public static byte[] ToJson(IEnumerable<WatchlistItem> items)
    {
        var payload = items.Select(i => new
        {
            list = i.ListType == WatchlistType.Watched ? "watched" : "want",
            listLabel = ListLabel(i.ListType),
            tmdbId = i.TmdbMovieId,
            title = i.Title,
            originalTitle = i.OriginalTitle,
            releaseYear = i.ReleaseYear,
            genres = WatchlistStatsCalculator.SplitGenres(i.GenresCsv),
            runtimeMinutes = i.RuntimeMinutes,
            tmdbVoteAverage = i.TmdbVoteAverage,
            personalRating = i.PersonalRating,
            personalNotes = i.PersonalNotes,
            watchedAt = i.WatchedAt,
            addedAt = i.AddedAt,
            posterUrl = i.PosterPath
        });

        return JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        // Evita injeção de fórmulas no Excel (=, +, -, @ no início da célula)
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !double.TryParse(value, NumberStyles.Any, PtBr, out _))
            value = "'" + value;

        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
