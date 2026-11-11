using VibeMovies.Api;

// Toda a configuração (serviços, segurança, banco, SPA) fica em VibeMoviesApp — reutilizada pelos testes de integração.
var app = await VibeMoviesApp.BuildAsync(args);
app.Run();

/// <summary>Exposto para testes de integração.</summary>
public partial class Program { }
