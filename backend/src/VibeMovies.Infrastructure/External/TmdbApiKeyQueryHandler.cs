namespace VibeMovies.Infrastructure.External;

/// <summary>
/// DelegatingHandler que converte o header interno X-Api-Key-V3 no parâmetro de query ?api_key= exigido pela API v3 do TMDB
/// quando o usuário utiliza uma API Key v3 (32 caracteres hexadecimais) em vez de um Bearer Token v4.
/// </summary>
public class TmdbApiKeyQueryHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.TryGetValues("X-Api-Key-V3", out var values))
        {
            var apiKey = values.FirstOrDefault();
            request.Headers.Remove("X-Api-Key-V3");

            if (!string.IsNullOrWhiteSpace(apiKey) && request.RequestUri is not null)
            {
                var uriBuilder = new UriBuilder(request.RequestUri);
                var existingQuery = uriBuilder.Query.TrimStart('?');
                uriBuilder.Query = string.IsNullOrEmpty(existingQuery)
                    ? $"api_key={Uri.EscapeDataString(apiKey)}"
                    : $"{existingQuery}&api_key={Uri.EscapeDataString(apiKey)}";
                request.RequestUri = uriBuilder.Uri;
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
