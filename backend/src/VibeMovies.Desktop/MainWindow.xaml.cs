using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Desktop;

public partial class MainWindow : Window
{
    private readonly IUserRepository _userRepository;
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly ITmdbService _tmdbService;
    private readonly ICinemaRadarService _cinemaRadarService;
    private readonly IAuthService _authService;

    private readonly HashSet<string> _selectedVibeIds = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<int, WatchlistItem> _userWatchlistByTmdbId = new();

    private Guid _currentUserId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private string _currentUserName = "Cinéfilo Vibe";
    private string _currentUserEmail = "demo@vibemovies.app";
    private string _currentCity = "Joinville/SC";
    private string? _runtimeTmdbApiKey;

    private string _currentWatchlistFilter = "All";
    private MovieCardDto? _pendingRatingMovie;
    private WatchlistItem? _pendingRatingWatchlistItem;
    private int _selectedModalRating = 5;
    private bool _isInitializing = true;

    private sealed record ComboOption<T>(string Label, T Value)
    {
        public override string ToString() => Label;
    }

    public MainWindow(
        IUserRepository userRepository,
        IWatchlistRepository watchlistRepository,
        ITmdbService tmdbService,
        ICinemaRadarService cinemaRadarService,
        IAuthService authService,
        string activeDbProvider)
    {
        InitializeComponent();

        _userRepository = userRepository;
        _watchlistRepository = watchlistRepository;
        _tmdbService = tmdbService;
        _cinemaRadarService = cinemaRadarService;
        _authService = authService;

        FooterDbText.Text = $"🗄️ Persistência Ativa: {activeDbProvider}";

        InitializeControls();
        Loaded += MainWindow_Loaded;
    }

    private void InitializeControls()
    {
        _isInitializing = true;

        // 1. Populate Vibe Chips
        RenderVibeChips();

        // 2. Populate Genre ComboBox
        GenreComboBox.Items.Clear();
        GenreComboBox.Items.Add(new ComboOption<int?>("Todos os Gêneros", null));
        foreach (var g in VibeCatalog.AllGenres)
        {
            GenreComboBox.Items.Add(new ComboOption<int?>($"{g.Icon} {g.Name}", g.Id));
        }
        GenreComboBox.SelectedIndex = 0;

        // 3. Populate Max Runtime ComboBox
        RuntimeComboBox.Items.Clear();
        RuntimeComboBox.Items.Add(new ComboOption<int?>("Qualquer duração", null));
        RuntimeComboBox.Items.Add(new ComboOption<int?>("Até 90 min", 90));
        RuntimeComboBox.Items.Add(new ComboOption<int?>("Até 120 min", 120));
        RuntimeComboBox.Items.Add(new ComboOption<int?>("Até 140 min", 140));
        RuntimeComboBox.Items.Add(new ComboOption<int?>("Até 160 min", 160));
        RuntimeComboBox.SelectedIndex = 0;

        // 4. Populate Streaming ComboBox
        StreamingComboBox.Items.Clear();
        StreamingComboBox.Items.Add(new ComboOption<string?>("Todos os Streamings", null));
        StreamingComboBox.Items.Add(new ComboOption<string?>("Netflix", "Netflix"));
        StreamingComboBox.Items.Add(new ComboOption<string?>("Amazon Prime Video", "Amazon Prime Video"));
        StreamingComboBox.Items.Add(new ComboOption<string?>("Max", "Max"));
        StreamingComboBox.Items.Add(new ComboOption<string?>("Disney+", "Disney+"));
        StreamingComboBox.Items.Add(new ComboOption<string?>("Globoplay", "Globoplay"));
        StreamingComboBox.Items.Add(new ComboOption<string?>("MUBI", "MUBI"));
        StreamingComboBox.SelectedIndex = 0;

        // 5. Populate Supported Cities in Cinema Radar
        RenderCityButtons();

        _isInitializing = false;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var demoUser = await _userRepository.GetByEmailAsync(_currentUserEmail);
        if (demoUser is not null)
        {
            _currentUserId = demoUser.Id;
            _currentUserName = demoUser.Name;
            _currentUserEmail = demoUser.Email;
            _currentCity = string.IsNullOrWhiteSpace(demoUser.DefaultCity) ? "Joinville/SC" : demoUser.DefaultCity;
            CustomCityTextBox.Text = _currentCity;
            UpdateUserHeaderButton();
        }

        await RefreshWatchlistIndexAsync();
        await LoadDiscoverMoviesAsync();
    }

    private void UpdateUserHeaderButton()
    {
        BtnUserProfile.Content = $"👤 {_currentUserName} ({_currentUserEmail})";
        AuthCurrentStatusText.Text = $"Conectado como: {_currentUserName} ({_currentUserEmail})";
    }

    // ==================== NAVIGATION ====================

    private async void NavExploreBtn_Click(object sender, RoutedEventArgs e)
    {
        SetActiveView("Explore");
        await RefreshWatchlistIndexAsync();
        await LoadDiscoverMoviesAsync();
    }

    private async void NavRadarBtn_Click(object sender, RoutedEventArgs e)
    {
        SetActiveView("Radar");
        await LoadCinemaRadarAsync(CustomCityTextBox.Text);
    }

    private async void NavWatchlistBtn_Click(object sender, RoutedEventArgs e)
    {
        SetActiveView("Watchlist");
        await LoadWatchlistViewAsync();
    }

    private void SetActiveView(string viewName)
    {
        ViewExplore.Visibility = viewName == "Explore" ? Visibility.Visible : Visibility.Collapsed;
        ViewRadar.Visibility = viewName == "Radar" ? Visibility.Visible : Visibility.Collapsed;
        ViewWatchlist.Visibility = viewName == "Watchlist" ? Visibility.Visible : Visibility.Collapsed;

        NavExploreBtn.Style = (Style)FindResource(viewName == "Explore" ? "PrimaryButtonStyle" : "SecondaryButtonStyle");
        NavRadarBtn.Style = (Style)FindResource(viewName == "Radar" ? "PrimaryButtonStyle" : "SecondaryButtonStyle");
        NavWatchlistBtn.Style = (Style)FindResource(viewName == "Watchlist" ? "PrimaryButtonStyle" : "SecondaryButtonStyle");
    }

    // ==================== VIEW 1: EXPLORAR POR VIBE ====================

    private void RenderVibeChips()
    {
        VibeChipsPanel.Children.Clear();

        foreach (var vibe in VibeCatalog.AllVibes)
        {
            bool isSelected = _selectedVibeIds.Contains(vibe.Id);
            var accentBrush = ParseHexBrush(vibe.AccentColor, "#7C3AED");

            var chipButton = new Button
            {
                Tag = vibe.Id,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 8, 12, 8),
                Background = isSelected ? ParseHexBrush("#261C47", "#261C47") : ParseHexBrush("#161928", "#161928"),
                BorderBrush = isSelected ? accentBrush : ParseHexBrush("#2A2F47", "#2A2F47"),
                BorderThickness = new Thickness(isSelected ? 1.8 : 1),
                ToolTip = $"{vibe.Subtitle}\n{vibe.Description}"
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text = vibe.Emoji,
                FontSize = 15,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock
            {
                Text = vibe.Name,
                FontSize = 12.5,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = isSelected ? Brushes.White : ParseHexBrush("#E2E8F0", "#E2E8F0")
            });
            textStack.Children.Add(new TextBlock
            {
                Text = vibe.Subtitle,
                FontSize = 10.5,
                Foreground = isSelected ? ParseHexBrush("#C4B5FD", "#C4B5FD") : ParseHexBrush("#64748B", "#64748B")
            });

            stack.Children.Add(textStack);

            if (isSelected)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = " ✓",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = accentBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                });
            }

            chipButton.Template = CreateChipControlTemplate();
            chipButton.Content = stack;
            chipButton.Click += VibeChip_Click;

            VibeChipsPanel.Children.Add(chipButton);
        }
    }

    private static ControlTemplate CreateChipControlTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);

        borderFactory.AppendChild(contentFactory);
        template.VisualTree = borderFactory;
        return template;
    }

    private async void VibeChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string vibeId)
        {
            if (!_selectedVibeIds.Remove(vibeId))
            {
                _selectedVibeIds.Add(vibeId);
            }

            RenderVibeChips();
            await LoadDiscoverMoviesAsync();
        }
    }

    private async void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        await LoadDiscoverMoviesAsync();
    }

    private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await LoadDiscoverMoviesAsync();
        }
    }

    private async void BtnApplySearch_Click(object sender, RoutedEventArgs e)
    {
        await LoadDiscoverMoviesAsync();
    }

    private async void BtnClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        _selectedVibeIds.Clear();
        GenreComboBox.SelectedIndex = 0;
        RuntimeComboBox.SelectedIndex = 0;
        StreamingComboBox.SelectedIndex = 0;
        SearchTextBox.Text = string.Empty;
        RenderVibeChips();
        _isInitializing = false;

        await LoadDiscoverMoviesAsync();
    }

    private async Task RefreshWatchlistIndexAsync()
    {
        var list = await _watchlistRepository.GetByUserIdAsync(_currentUserId);
        _userWatchlistByTmdbId = list
            .GroupBy(i => i.TmdbMovieId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private async Task LoadDiscoverMoviesAsync()
    {
        try
        {
            FooterStatusText.Text = "Consultando motor de Vibes e provedores de Streaming (BR)...";

            var selectedGenre = (GenreComboBox.SelectedItem as ComboOption<int?>)?.Value;
            var selectedRuntime = (RuntimeComboBox.SelectedItem as ComboOption<int?>)?.Value;
            var selectedStreaming = (StreamingComboBox.SelectedItem as ComboOption<string?>)?.Value;
            var searchText = SearchTextBox.Text?.Trim();

            var query = new VibeDiscoverQuery
            {
                VibeIds = _selectedVibeIds.ToList(),
                GenreIds = selectedGenre.HasValue ? new List<int> { selectedGenre.Value } : new List<int>(),
                MaxRuntimeMinutes = selectedRuntime,
                WatchProviderFilter = selectedStreaming,
                SearchText = searchText,
                Region = "BR"
            };

            var result = await _tmdbService.DiscoverByVibeAsync(query, _runtimeTmdbApiKey);

            FormulaSummaryText.Text = result.AppliedFormulaSummary;
            ResultCountLabel.Text = $"{result.TotalResults} filme(s) encontrado(s)";

            ApiModeBadgeText.Text = result.UsingLiveTmdbApi
                ? "🌐 API TMDB Ao Vivo (v3/v4) + JustWatch BR"
                : "⚡ Catálogo Curado Local + JustWatch BR";

            RenderMovieCards(result.Movies);

            FooterStatusText.Text = $"Exibindo {result.Movies.Count} filmes • Região: BR";
        }
        catch (Exception ex)
        {
            FooterStatusText.Text = $"Erro ao carregar filmes: {ex.Message}";
        }
    }

    private void RenderMovieCards(List<MovieCardDto> movies)
    {
        MoviesCardsPanel.Children.Clear();

        if (movies.Count == 0)
        {
            var emptyBorder = new Border
            {
                Background = ParseHexBrush("#12141F", "#12141F"),
                BorderBrush = ParseHexBrush("#262A40", "#262A40"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(32),
                Margin = new Thickness(0, 16, 0, 16),
                Width = 640
            };

            var emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            emptyStack.Children.Add(new TextBlock
            {
                Text = "🎞️ Nenhum filme encontrado para essa combinação de filtros",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            emptyStack.Children.Add(new TextBlock
            {
                Text = "Experimente desmarcar alguma Vibe ou clicar em 'Limpar Filtros' para ampliar a busca.",
                FontSize = 13,
                Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            emptyBorder.Child = emptyStack;
            MoviesCardsPanel.Children.Add(emptyBorder);
            return;
        }

        foreach (var movie in movies)
        {
            MoviesCardsPanel.Children.Add(BuildMovieCard(movie));
        }
    }

    private UIElement BuildMovieCard(MovieCardDto movie)
    {
        var cardBorder = new Border
        {
            Width = 420,
            Background = ParseHexBrush("#12141F", "#12141F"),
            BorderBrush = ParseHexBrush("#232840", "#232840"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 0, 16, 16),
            Padding = new Thickness(14)
        };

        var outerStack = new StackPanel();

        // Top section: Poster + Metadata
        var topGrid = new Grid();
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(116) });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Poster container
        var posterElement = BuildPosterImageElement(movie.PosterUrl, movie.Title, 110, 164);
        Grid.SetColumn(posterElement, 0);
        topGrid.Children.Add(posterElement);

        // Right metadata stack
        var metaStack = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
        Grid.SetColumn(metaStack, 1);

        // Badges row: Vibe Match Score + Rating + Watchlist Status
        var badgesWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
        badgesWrap.Children.Add(CreatePillBadge($"🔥 {movie.VibeMatchScore}% Vibe", "#132A23", "#10B981"));
        badgesWrap.Children.Add(CreatePillBadge($"★ {movie.VoteAverage:0.0}", "#2B2111", "#F59E0B"));

        if (movie.RuntimeMinutes.HasValue && movie.RuntimeMinutes.Value > 0)
        {
            badgesWrap.Children.Add(CreatePillBadge($"⏱ {movie.RuntimeMinutes.Value} min", "#1A1E30", "#94A3B8"));
        }

        if (_userWatchlistByTmdbId.TryGetValue(movie.TmdbId, out var existingItem))
        {
            if (existingItem.ListType == WatchlistType.Watched)
            {
                string stars = existingItem.PersonalRating.HasValue ? $" ({existingItem.PersonalRating}★)" : "";
                badgesWrap.Children.Add(CreatePillBadge($"✅ Já Vi{stars}", "#064E3B", "#34D399"));
            }
            else
            {
                badgesWrap.Children.Add(CreatePillBadge("📌 Na Fila", "#2E1065", "#C4B5FD"));
            }
        }

        metaStack.Children.Add(badgesWrap);

        // Movie Title & Year
        string yearSuffix = movie.ReleaseYear.HasValue ? $" ({movie.ReleaseYear.Value})" : "";
        metaStack.Children.Add(new TextBlock
        {
            Text = $"{movie.Title}{yearSuffix}",
            FontSize = 15.5,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        });

        // Genres
        if (movie.Genres.Count > 0)
        {
            metaStack.Children.Add(new TextBlock
            {
                Text = string.Join(" • ", movie.Genres),
                FontSize = 11.5,
                Foreground = ParseHexBrush("#A855F7", "#A855F7"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 3, 0, 6),
                TextWrapping = TextWrapping.Wrap
            });
        }

        // Synopsis
        metaStack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(movie.Overview) ? "Sinopse indisponível." : movie.Overview,
            FontSize = 12,
            Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 72,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        topGrid.Children.Add(metaStack);
        outerStack.Children.Add(topGrid);

        // Streaming / Watch Providers Section (Onde Assistir BR)
        var providersBox = new Border
        {
            Background = ParseHexBrush("#0D0F18", "#0D0F18"),
            BorderBrush = ParseHexBrush("#1E243B", "#1E243B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 12, 0, 10)
        };

        var providersStack = new StackPanel();
        providersStack.Children.Add(new TextBlock
        {
            Text = "📺 ONDE ASSISTIR (BR — JUSTWATCH):",
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Foreground = ParseHexBrush("#64748B", "#64748B"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        string flatrateNames = movie.WatchProviders.Flatrate.Count > 0
            ? string.Join(", ", movie.WatchProviders.Flatrate.Select(p => p.ProviderName))
            : "Indisponível em assinatura fixa";

        providersStack.Children.Add(CreateProviderRow("Assinatura (Stream):", flatrateNames, "#10B981"));

        if (movie.WatchProviders.Rent.Count > 0)
        {
            string rentNames = string.Join(", ", movie.WatchProviders.Rent.Select(p => p.ProviderName));
            providersStack.Children.Add(CreateProviderRow("Aluguel (Rent):", rentNames, "#38BDF8"));
        }

        if (movie.WatchProviders.Buy.Count > 0)
        {
            string buyNames = string.Join(", ", movie.WatchProviders.Buy.Select(p => p.ProviderName));
            providersStack.Children.Add(CreateProviderRow("Compra (Buy):", buyNames, "#F59E0B"));
        }

        providersBox.Child = providersStack;
        outerStack.Children.Add(providersBox);

        // Action Buttons Row
        var actionsGrid = new UniformGrid { Columns = 3 };

        var btnWant = new Button
        {
            Content = "📌 Quero Ver",
            Style = (Style)FindResource("SecondaryButtonStyle"),
            Margin = new Thickness(0, 0, 5, 0),
            Padding = new Thickness(6, 7, 6, 7),
            FontSize = 11.5
        };
        btnWant.Click += async (_, _) => await SaveMovieAsWantToWatchAsync(movie);

        var btnWatched = new Button
        {
            Content = "✅ Já Vi / Avaliar",
            Style = (Style)FindResource("PrimaryButtonStyle"),
            Margin = new Thickness(3, 0, 3, 0),
            Padding = new Thickness(6, 7, 6, 7),
            FontSize = 11.5
        };
        btnWatched.Click += (_, _) => OpenRatingModalForMovie(movie);

        var btnDetails = new Button
        {
            Content = "🔍 Detalhes",
            Style = (Style)FindResource("SecondaryButtonStyle"),
            Margin = new Thickness(5, 0, 0, 0),
            Padding = new Thickness(6, 7, 6, 7),
            FontSize = 11.5
        };
        btnDetails.Click += async (_, _) => await OpenMovieDetailsModalAsync(movie);

        actionsGrid.Children.Add(btnWant);
        actionsGrid.Children.Add(btnWatched);
        actionsGrid.Children.Add(btnDetails);

        outerStack.Children.Add(actionsGrid);
        cardBorder.Child = outerStack;

        return cardBorder;
    }

    private static UIElement CreateProviderRow(string label, string providers, string valueHexColor)
    {
        var dock = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
        dock.Children.Add(new TextBlock
        {
            Text = $"{label} ",
            FontSize = 11.5,
            Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
            FontWeight = FontWeights.SemiBold
        });
        dock.Children.Add(new TextBlock
        {
            Text = providers,
            FontSize = 11.5,
            Foreground = ParseHexBrush(valueHexColor, "#E2E8F0"),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        return dock;
    }

    private async Task SaveMovieAsWantToWatchAsync(MovieCardDto movie)
    {
        var item = new WatchlistItem
        {
            UserId = _currentUserId,
            TmdbMovieId = movie.TmdbId,
            Title = movie.Title,
            OriginalTitle = movie.OriginalTitle,
            PosterPath = movie.PosterUrl,
            BackdropPath = movie.BackdropUrl,
            ReleaseYear = movie.ReleaseYear,
            Overview = movie.Overview,
            ListType = WatchlistType.WantToWatch,
            GenresCsv = string.Join(", ", movie.Genres),
            RuntimeMinutes = movie.RuntimeMinutes,
            TmdbVoteAverage = movie.VoteAverage
        };

        await _watchlistRepository.UpsertAsync(item);
        await RefreshWatchlistIndexAsync();
        await LoadDiscoverMoviesAsync();

        FooterStatusText.Text = $"📌 '{movie.Title}' salvo na fila 'Quero Ver'!";
    }

    // ==================== VIEW 2: RADAR DE CINEMAS LOCAIS ====================

    private void RenderCityButtons()
    {
        CityButtonsPanel.Children.Clear();
        foreach (var city in _cinemaRadarService.GetSupportedCities())
        {
            var btn = new Button
            {
                Content = $"📍 {city}",
                Tag = city,
                Style = (Style)FindResource("SecondaryButtonStyle"),
                Margin = new Thickness(0, 0, 8, 6)
            };
            btn.Click += async (_, _) =>
            {
                CustomCityTextBox.Text = city;
                await LoadCinemaRadarAsync(city);
            };
            CityButtonsPanel.Children.Add(btn);
        }
    }

    private async void BtnSearchRadar_Click(object sender, RoutedEventArgs e)
    {
        await LoadCinemaRadarAsync(CustomCityTextBox.Text);
    }

    private async Task LoadCinemaRadarAsync(string city)
    {
        try
        {
            _currentCity = string.IsNullOrWhiteSpace(city) ? "Joinville/SC" : city.Trim();
            FooterStatusText.Text = $"Extraindo programação de cinemas para {_currentCity} via HtmlAgilityPack...";

            var response = await _cinemaRadarService.GetLocalRadarAsync(_currentCity, _runtimeTmdbApiKey);

            RadarSourceText.Text = $"🔧 Fonte: {response.ScraperSource}";
            RadarTimestampText.Text = $"Atualizado às {response.ScrapedAt.ToLocalTime():HH:mm:ss}";

            RadarMoviesPanel.Children.Clear();

            foreach (var item in response.MoviesInTheaters)
            {
                RadarMoviesPanel.Children.Add(BuildRadarMovieCard(item));
            }

            FooterStatusText.Text = $"Radar concluído: {response.MoviesInTheaters.Count} filmes em cartaz em {response.City}/{response.State}";
        }
        catch (Exception ex)
        {
            FooterStatusText.Text = $"Erro no Radar de Cinemas: {ex.Message}";
        }
    }

    private UIElement BuildRadarMovieCard(NowPlayingMovieWithShowtimesDto entry)
    {
        var card = new Border
        {
            Background = ParseHexBrush("#12141F", "#12141F"),
            BorderBrush = ParseHexBrush("#23273D", "#23273D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 14)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var poster = BuildPosterImageElement(entry.Movie.PosterUrl, entry.Movie.Title, 110, 164);
        Grid.SetColumn(poster, 0);
        grid.Children.Add(poster);

        var contentStack = new StackPanel { Margin = new Thickness(16, 0, 0, 0) };
        Grid.SetColumn(contentStack, 1);

        var headerWrap = new WrapPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
        headerWrap.Children.Add(new TextBlock
        {
            Text = entry.Movie.Title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 10, 0)
        });
        headerWrap.Children.Add(CreatePillBadge($"🔞 {entry.AgeRating}", "#2E1065", "#C4B5FD"));
        headerWrap.Children.Add(CreatePillBadge($"★ {entry.Movie.VoteAverage:0.0}", "#2B2111", "#F59E0B"));
        if (entry.Movie.RuntimeMinutes.HasValue)
        {
            headerWrap.Children.Add(CreatePillBadge($"⏱ {entry.Movie.RuntimeMinutes.Value} min", "#1A1E30", "#94A3B8"));
        }

        contentStack.Children.Add(headerWrap);

        contentStack.Children.Add(new TextBlock
        {
            Text = entry.Movie.Overview,
            FontSize = 12.5,
            Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Venues and Showtimes
        foreach (var venue in entry.Venues)
        {
            var venueBorder = new Border
            {
                Background = ParseHexBrush("#0D0F18", "#0D0F18"),
                BorderBrush = ParseHexBrush("#1F2437", "#1F2437"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var venueStack = new StackPanel();
            venueStack.Children.Add(new TextBlock
            {
                Text = $"🏛️ {venue.CinemaName} ({venue.CinemaChain})",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = ParseHexBrush("#E2E8F0", "#E2E8F0")
            });
            venueStack.Children.Add(new TextBlock
            {
                Text = venue.Address,
                FontSize = 11.5,
                Foreground = ParseHexBrush("#64748B", "#64748B"),
                Margin = new Thickness(0, 2, 0, 8)
            });

            var sessionsWrap = new WrapPanel();
            foreach (var session in venue.Sessions)
            {
                string badgeText = $"🕒 {session.Time} • {session.AudioType} • {session.RoomType}";
                sessionsWrap.Children.Add(CreatePillBadge(badgeText, "#132A23", "#10B981"));
            }

            venueStack.Children.Add(sessionsWrap);
            venueBorder.Child = venueStack;
            contentStack.Children.Add(venueBorder);
        }

        grid.Children.Add(contentStack);
        card.Child = grid;
        return card;
    }

    // ==================== VIEW 3: DIÁRIO DE BORDO (WATCHLIST) ====================

    private async void BtnWatchlistFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string filterTag)
        {
            _currentWatchlistFilter = filterTag;

            BtnFilterAll.Style = (Style)FindResource(filterTag == "All" ? "PrimaryButtonStyle" : "SecondaryButtonStyle");
            BtnFilterWatched.Style = (Style)FindResource(filterTag == "Watched" ? "PrimaryButtonStyle" : "SecondaryButtonStyle");
            BtnFilterWant.Style = (Style)FindResource(filterTag == "WantToWatch" ? "PrimaryButtonStyle" : "SecondaryButtonStyle");

            await LoadWatchlistViewAsync();
        }
    }

    private async Task LoadWatchlistViewAsync()
    {
        var allItems = await _watchlistRepository.GetByUserIdAsync(_currentUserId);
        _userWatchlistByTmdbId = allItems
            .GroupBy(i => i.TmdbMovieId)
            .ToDictionary(g => g.Key, g => g.First());

        var watchedItems = allItems.Where(i => i.ListType == WatchlistType.Watched).ToList();
        var wantItems = allItems.Where(i => i.ListType == WatchlistType.WantToWatch).ToList();

        int totalMinutes = watchedItems.Sum(i => i.RuntimeMinutes ?? 120);
        double avgRating = watchedItems.Where(i => i.PersonalRating.HasValue).Select(i => (double)i.PersonalRating!.Value).DefaultIfEmpty(0).Average();

        StatWatchedCount.Text = watchedItems.Count.ToString();
        StatWantCount.Text = wantItems.Count.ToString();
        StatAvgRating.Text = avgRating > 0 ? $"{avgRating:0.1} ★" : "—";
        StatTotalTime.Text = $"{totalMinutes / 60}h {totalMinutes % 60}m";
        StatMinutesSub.Text = $"{totalMinutes} minutos de cinema assistidos";

        List<WatchlistItem> filtered = _currentWatchlistFilter switch
        {
            "Watched" => watchedItems,
            "WantToWatch" => wantItems,
            _ => allItems
        };

        WatchlistCountText.Text = $"{filtered.Count} registro(s) exibido(s)";
        WatchlistItemsPanel.Children.Clear();

        if (filtered.Count == 0)
        {
            var emptyBorder = new Border
            {
                Background = ParseHexBrush("#12141F", "#12141F"),
                BorderBrush = ParseHexBrush("#23273D", "#23273D"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(28)
            };
            emptyBorder.Child = new TextBlock
            {
                Text = "Nenhum filme registrado nesta categoria ainda. Explore por Vibe para adicionar títulos!",
                FontSize = 14,
                Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            WatchlistItemsPanel.Children.Add(emptyBorder);
            return;
        }

        foreach (var item in filtered)
        {
            WatchlistItemsPanel.Children.Add(BuildWatchlistItemCard(item));
        }
    }

    private UIElement BuildWatchlistItemCard(WatchlistItem item)
    {
        var card = new Border
        {
            Background = ParseHexBrush("#12141F", "#12141F"),
            BorderBrush = ParseHexBrush("#23273D", "#23273D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var poster = BuildPosterImageElement(item.PosterPath, item.Title, 88, 132);
        Grid.SetColumn(poster, 0);
        grid.Children.Add(poster);

        var infoStack = new StackPanel { Margin = new Thickness(14, 0, 14, 0) };
        Grid.SetColumn(infoStack, 1);

        var badgesWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
        if (item.ListType == WatchlistType.Watched)
        {
            badgesWrap.Children.Add(CreatePillBadge("✅ JÁ ASSISTIDO", "#064E3B", "#34D399"));
            int stars = Math.Clamp(item.PersonalRating ?? 5, 1, 5);
            string starsStr = new string('★', stars) + new string('☆', 5 - stars);
            badgesWrap.Children.Add(CreatePillBadge($"{starsStr} ({stars}/5)", "#2B2111", "#F59E0B"));
            if (item.WatchedAt.HasValue)
            {
                badgesWrap.Children.Add(CreatePillBadge($"📅 Visto em {item.WatchedAt.Value.ToLocalTime():dd/MM/yyyy}", "#1A1E30", "#94A3B8"));
            }
        }
        else
        {
            badgesWrap.Children.Add(CreatePillBadge("📌 QUERO VER (NA FILA)", "#2E1065", "#C4B5FD"));
        }

        if (item.RuntimeMinutes.HasValue)
        {
            badgesWrap.Children.Add(CreatePillBadge($"⏱ {item.RuntimeMinutes.Value} min", "#1A1E30", "#94A3B8"));
        }

        infoStack.Children.Add(badgesWrap);

        string yearStr = item.ReleaseYear.HasValue ? $" ({item.ReleaseYear.Value})" : "";
        infoStack.Children.Add(new TextBlock
        {
            Text = $"{item.Title}{yearStr}",
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });

        if (!string.IsNullOrWhiteSpace(item.GenresCsv))
        {
            infoStack.Children.Add(new TextBlock
            {
                Text = item.GenresCsv,
                FontSize = 12,
                Foreground = ParseHexBrush("#A855F7", "#A855F7"),
                Margin = new Thickness(0, 2, 0, 6)
            });
        }

        if (!string.IsNullOrWhiteSpace(item.PersonalNotes))
        {
            var noteBorder = new Border
            {
                Background = ParseHexBrush("#0D0F18", "#0D0F18"),
                BorderBrush = ParseHexBrush("#262A40", "#262A40"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 4, 0, 4)
            };
            noteBorder.Child = new TextBlock
            {
                Text = $"💬 \"{item.PersonalNotes}\"",
                FontSize = 12.5,
                FontStyle = FontStyles.Italic,
                Foreground = ParseHexBrush("#E2E8F0", "#E2E8F0"),
                TextWrapping = TextWrapping.Wrap
            };
            infoStack.Children.Add(noteBorder);
        }
        else if (!string.IsNullOrWhiteSpace(item.Overview))
        {
            infoStack.Children.Add(new TextBlock
            {
                Text = item.Overview,
                FontSize = 12,
                Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
                TextWrapping = TextWrapping.Wrap
            });
        }

        grid.Children.Add(infoStack);

        // Right Action Buttons
        var actionsStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(actionsStack, 2);

        var btnRateOrEdit = new Button
        {
            Content = item.ListType == WatchlistType.WantToWatch ? "✅ Marcar Já Vi / Avaliar" : "✏️ Editar Nota / Crítica",
            Style = (Style)FindResource("PrimaryButtonStyle"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        btnRateOrEdit.Click += (_, _) => OpenRatingModalForWatchlistItem(item);
        actionsStack.Children.Add(btnRateOrEdit);

        var btnRemove = new Button
        {
            Content = "🗑️ Remover",
            Style = (Style)FindResource("SecondaryButtonStyle")
        };
        btnRemove.Click += async (_, _) =>
        {
            await _watchlistRepository.DeleteAsync(item.Id, _currentUserId);
            await LoadWatchlistViewAsync();
            FooterStatusText.Text = $"🗑️ '{item.Title}' removido do Diário de Bordo.";
        };
        actionsStack.Children.Add(btnRemove);

        grid.Children.Add(actionsStack);
        card.Child = grid;
        return card;
    }

    // ==================== MODALS (RATING, DETAILS, TMDB KEY, AUTH) ====================

    private void OpenRatingModalForMovie(MovieCardDto movie)
    {
        _pendingRatingMovie = movie;
        _pendingRatingWatchlistItem = null;

        if (_userWatchlistByTmdbId.TryGetValue(movie.TmdbId, out var existing))
        {
            _selectedModalRating = existing.PersonalRating ?? 5;
            RatingNotesTextBox.Text = existing.PersonalNotes ?? string.Empty;
        }
        else
        {
            _selectedModalRating = 5;
            RatingNotesTextBox.Text = string.Empty;
        }

        RatingModalMovieTitle.Text = $"{movie.Title} ({movie.ReleaseYear})";
        RenderInteractiveStarButtons();
        ShowModal(RatingModalCard);
    }

    private void OpenRatingModalForWatchlistItem(WatchlistItem item)
    {
        _pendingRatingWatchlistItem = item;
        _pendingRatingMovie = null;
        _selectedModalRating = item.PersonalRating ?? 5;
        RatingNotesTextBox.Text = item.PersonalNotes ?? string.Empty;

        RatingModalMovieTitle.Text = $"{item.Title} ({item.ReleaseYear})";
        RenderInteractiveStarButtons();
        ShowModal(RatingModalCard);
    }

    private void RenderInteractiveStarButtons()
    {
        RatingStarsPanel.Children.Clear();
        for (int star = 1; star <= 5; star++)
        {
            int capturedStar = star;
            bool isLit = star <= _selectedModalRating;

            var btn = new Button
            {
                Content = isLit ? $"★ {star}" : $"☆ {star}",
                Width = 64,
                Height = 38,
                Margin = new Thickness(0, 0, 8, 0),
                Background = isLit ? ParseHexBrush("#3B280C", "#3B280C") : ParseHexBrush("#1A1D2D", "#1A1D2D"),
                BorderBrush = isLit ? ParseHexBrush("#F59E0B", "#F59E0B") : ParseHexBrush("#2E334D", "#2E334D"),
                Foreground = isLit ? ParseHexBrush("#FBBF24", "#FBBF24") : ParseHexBrush("#94A3B8", "#94A3B8"),
                BorderThickness = new Thickness(1.5),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Template = CreateChipControlTemplate()
            };

            btn.Click += (_, _) =>
            {
                _selectedModalRating = capturedStar;
                RenderInteractiveStarButtons();
            };

            RatingStarsPanel.Children.Add(btn);
        }
    }

    private async void BtnSaveRating_Click(object sender, RoutedEventArgs e)
    {
        string? notes = string.IsNullOrWhiteSpace(RatingNotesTextBox.Text) ? null : RatingNotesTextBox.Text.Trim();

        if (_pendingRatingMovie is not null)
        {
            var m = _pendingRatingMovie;
            var item = new WatchlistItem
            {
                UserId = _currentUserId,
                TmdbMovieId = m.TmdbId,
                Title = m.Title,
                OriginalTitle = m.OriginalTitle,
                PosterPath = m.PosterUrl,
                BackdropPath = m.BackdropUrl,
                ReleaseYear = m.ReleaseYear,
                Overview = m.Overview,
                ListType = WatchlistType.Watched,
                PersonalRating = _selectedModalRating,
                PersonalNotes = notes,
                WatchedAt = DateTime.UtcNow,
                GenresCsv = string.Join(", ", m.Genres),
                RuntimeMinutes = m.RuntimeMinutes,
                TmdbVoteAverage = m.VoteAverage
            };

            await _watchlistRepository.UpsertAsync(item);
            FooterStatusText.Text = $"✅ '{m.Title}' avaliado com {_selectedModalRating}★ no Diário de Bordo!";
        }
        else if (_pendingRatingWatchlistItem is not null)
        {
            var w = _pendingRatingWatchlistItem;
            await _watchlistRepository.UpdateAsync(
                w.Id,
                _currentUserId,
                new UpdateWatchlistItemDto(WatchlistType.Watched, _selectedModalRating, notes, DateTime.UtcNow));

            FooterStatusText.Text = $"✅ Avaliação de '{w.Title}' atualizada ({_selectedModalRating}★)!";
        }

        CloseAllModals();
        await RefreshWatchlistIndexAsync();

        if (ViewWatchlist.Visibility == Visibility.Visible)
        {
            await LoadWatchlistViewAsync();
        }
        else if (ViewExplore.Visibility == Visibility.Visible)
        {
            await LoadDiscoverMoviesAsync();
        }
    }

    private async Task OpenMovieDetailsModalAsync(MovieCardDto cardMovie)
    {
        var detailed = await _tmdbService.GetMovieDetailsAsync(cardMovie.TmdbId, "BR", _runtimeTmdbApiKey) ?? cardMovie;

        DetailsTitleText.Text = detailed.Title;
        DetailsSubtitleText.Text = $"{detailed.OriginalTitle} • {detailed.ReleaseYear} • {detailed.RuntimeMinutes ?? 120} min • ★ {detailed.VoteAverage:0.0} ({detailed.VoteCount} votos)";
        DetailsOverviewText.Text = detailed.Overview;

        DetailsDirectorText.Text = $"Direção: {(string.IsNullOrWhiteSpace(detailed.Director) ? "Informação no catálogo TMDB" : detailed.Director)}";
        DetailsCastText.Text = detailed.Cast.Count > 0
            ? $"Elenco: {string.Join(", ", detailed.Cast)}"
            : "Elenco principal registrado na base TMDB";

        DetailsGenresText.Text = detailed.Genres.Count > 0
            ? $"Gêneros: {string.Join(", ", detailed.Genres)}"
            : "Gêneros: Cinema";
        DetailsKeywordsText.Text = detailed.Keywords.Count > 0
            ? $"Keywords: {string.Join(", ", detailed.Keywords)}"
            : $"Vibes associadas: {string.Join(", ", detailed.MatchedVibes)}";

        var sb = new System.Text.StringBuilder();
        var wp = detailed.WatchProviders;
        sb.AppendLine($"• Assinatura (Stream): {(wp.Flatrate.Count > 0 ? string.Join(", ", wp.Flatrate.Select(x => x.ProviderName)) : "Não listado em assinatura fixa")}");
        sb.AppendLine($"• Aluguel Digital (Rent): {(wp.Rent.Count > 0 ? string.Join(", ", wp.Rent.Select(x => x.ProviderName)) : "Consulte lojas digitais")}");
        sb.Append($"• Compra Digital (Buy): {(wp.Buy.Count > 0 ? string.Join(", ", wp.Buy.Select(x => x.ProviderName)) : "Consulte lojas digitais")}");

        DetailsProvidersText.Text = sb.ToString();
        ShowModal(DetailsModalCard);
    }

    private void BtnTmdbKey_Click(object sender, RoutedEventArgs e)
    {
        TmdbApiKeyTextBox.Text = _runtimeTmdbApiKey ?? string.Empty;
        ShowModal(TmdbKeyModalCard);
    }

    private async void BtnSaveTmdbKey_Click(object sender, RoutedEventArgs e)
    {
        _runtimeTmdbApiKey = string.IsNullOrWhiteSpace(TmdbApiKeyTextBox.Text) ? null : TmdbApiKeyTextBox.Text.Trim();
        CloseAllModals();
        await LoadDiscoverMoviesAsync();
    }

    private async void BtnClearTmdbKey_Click(object sender, RoutedEventArgs e)
    {
        _runtimeTmdbApiKey = null;
        TmdbApiKeyTextBox.Text = string.Empty;
        CloseAllModals();
        await LoadDiscoverMoviesAsync();
    }

    private void BtnUserProfile_Click(object sender, RoutedEventArgs e)
    {
        AuthFeedbackText.Text = string.Empty;
        ShowModal(AuthModalCard);
    }

    private async void BtnLoginUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var res = await _authService.LoginAsync(new LoginRequestDto(AuthEmailTextBox.Text, AuthPasswordTextBox.Text));
            if (res is null)
            {
                AuthFeedbackText.Text = "E-mail ou senha inválidos.";
                return;
            }

            _currentUserId = res.UserId;
            _currentUserName = res.Name;
            _currentUserEmail = res.Email;
            _currentCity = res.DefaultCity;
            CustomCityTextBox.Text = _currentCity;

            UpdateUserHeaderButton();
            CloseAllModals();
            await RefreshWatchlistIndexAsync();
            await LoadDiscoverMoviesAsync();
            if (ViewWatchlist.Visibility == Visibility.Visible)
            {
                await LoadWatchlistViewAsync();
            }
        }
        catch (Exception ex)
        {
            AuthFeedbackText.Text = ex.Message;
        }
    }

    private async void BtnRegisterUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var res = await _authService.RegisterAsync(new RegisterRequestDto(
                AuthNameTextBox.Text,
                AuthEmailTextBox.Text,
                AuthPasswordTextBox.Text,
                "BR",
                _currentCity));

            _currentUserId = res.UserId;
            _currentUserName = res.Name;
            _currentUserEmail = res.Email;
            UpdateUserHeaderButton();
            CloseAllModals();
            await RefreshWatchlistIndexAsync();
            await LoadDiscoverMoviesAsync();
            if (ViewWatchlist.Visibility == Visibility.Visible)
            {
                await LoadWatchlistViewAsync();
            }
        }
        catch (Exception ex)
        {
            AuthFeedbackText.Text = ex.Message;
        }
    }

    private void ShowModal(FrameworkElement modalCard)
    {
        RatingModalCard.Visibility = Visibility.Collapsed;
        DetailsModalCard.Visibility = Visibility.Collapsed;
        TmdbKeyModalCard.Visibility = Visibility.Collapsed;
        AuthModalCard.Visibility = Visibility.Collapsed;

        modalCard.Visibility = Visibility.Visible;
        ModalBackdrop.Visibility = Visibility.Visible;
    }

    private void CloseAllModals()
    {
        ModalBackdrop.Visibility = Visibility.Collapsed;
        RatingModalCard.Visibility = Visibility.Collapsed;
        DetailsModalCard.Visibility = Visibility.Collapsed;
        TmdbKeyModalCard.Visibility = Visibility.Collapsed;
        AuthModalCard.Visibility = Visibility.Collapsed;
    }

    private void BtnCloseModal_Click(object sender, RoutedEventArgs e)
    {
        CloseAllModals();
    }

    // ==================== UI HELPERS ====================

    private static UIElement BuildPosterImageElement(string? posterUrl, string title, double width, double height)
    {
        var container = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(8),
            Background = ParseHexBrush("#1A1E30", "#1A1E30"),
            BorderBrush = ParseHexBrush("#2A2F47", "#2A2F47"),
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };

        var grid = new Grid();

        var fallbackStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(6)
        };
        fallbackStack.Children.Add(new TextBlock
        {
            Text = "🎬",
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        });
        fallbackStack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = ParseHexBrush("#94A3B8", "#94A3B8"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        grid.Children.Add(fallbackStack);

        if (!string.IsNullOrWhiteSpace(posterUrl) && Uri.TryCreate(posterUrl, UriKind.Absolute, out var uri))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var img = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill
                };
                grid.Children.Add(img);
            }
            catch
            {
                // Mantém o fallback visual elegante caso não haja internet
            }
        }

        container.Child = grid;
        return container;
    }

    private static Border CreatePillBadge(string text, string bgHex, string fgHex)
    {
        return new Border
        {
            Background = ParseHexBrush(bgHex, "#1A1D2D"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(7, 3, 7, 3),
            Margin = new Thickness(0, 0, 6, 4),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = ParseHexBrush(fgHex, "#F8FAFC")
            }
        };
    }

    private static SolidColorBrush ParseHexBrush(string hex, string fallbackHex)
    {
        try
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
        catch
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(fallbackHex)!;
        }
    }
}
