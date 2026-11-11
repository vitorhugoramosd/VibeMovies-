# 🎬 VibeMovies — Buscador por Vibe & Rastreador de Filmes

Aplicação web completa para amantes de cinema escolherem filmes com base no **"clima" ou "vibe"** do momento, descobrirem **onde assistir** (streamings incluídos na assinatura, aluguel ou compra via JustWatch/TMDB), consultarem o **Radar de Cinemas Locais** (em cartaz + sessões por cidade) e gerenciarem seu **Diário de Bordo** (*Quero Ver* e *Já Vi* com avaliação em estrelas e notas pessoais).

> Sem uso de APIs pagas de IA: toda a inteligência de recomendação utiliza cruzamento determinístico de metadados, **Keywords IDs oficiais do TMDB**, filtros temporais, duração máxima e disponibilidade regional.

---

## 🏗️ Arquitetura e Stack Tecnológico

### Backend (.NET Clean Architecture)
* **Framework:** ASP.NET Core Web API (C#)
* **Arquitetura em Camadas (Clean Architecture):**
  * `VibeMovies.Domain`: Entidades de domínio (`ApplicationUser`, `WatchlistItem`, `CachedMovie`), modelos de Vibe, DTOs e contratos.
  * `VibeMovies.Application`: Casos de uso, motor de mapeamento `VibeCatalog` (Vibes -> TMDB Keywords IDs, filtros de década, nota mínima e gêneros), `VibeDiscoveryService` e `AuthService` (JWT).
  * `VibeMovies.Infrastructure`: Persistência com **Entity Framework Core** (suporte a **PostgreSQL** via `Npgsql` e fallback automático para SQLite local), `TmdbHttpClientService` (`IHttpClientFactory` + cache de filmes/provedores em banco) e `CinemaRadarScraperService` (`HtmlAgilityPack` para scraping de sessões locais + cruzamento com `Now Playing`).
  * `VibeMovies.Api`: Controllers RESTful (`MoviesController`, `WatchlistController`, `CinemaRadarController`, `AuthController`), autenticação JWT Bearer, Swagger/OpenAPI e Data Seeder.

### Frontend (React + TypeScript + Vite + Tailwind CSS)
* **Interface:** Design cinematográfico escuro (*Dark Cinema UI*) focado nos pôsteres, filtros rápidos de Vibe, badges de streaming em tempo real e modais ricos de detalhes.
* **Módulos:**
  1. **Motor de Busca por "Vibe":** Seleção combinada de Vibes (ex: *Plot Twist*, *Cyberpunk*, *Pós-apocalíptico*, *Anos 90*, *Vingança*, *Emocionante*, *Mind-Bending*), Gênero, Duração Máxima (`<= 90m`, `<= 120m`, `<= 150m`, etc.) e Streaming.
  2. **Onde Assistir (Watch Providers):** Exibição clara de plataformas por **Assinatura (Stream)**, **Aluguel (Rent)** e **Compra (Buy)** na região selecionada (`BR`).
  3. **Radar de Cinemas Locais:** Busca por cidade (ex: *Joinville/SC*, *Curitiba/PR*, *São Paulo/SP*, *Florianópolis/SC*) combinando os filmes *Em Cartaz* (`/movie/now_playing`) com horários e salas locais via scraper `HtmlAgilityPack`.
  4. **Diário de Bordo (Watchlist & Histórico):** Autenticação JWT + listas **Quero Ver** e **Já Vi** (com nota de 1 a 5 estrelas, data assistida e resenha pessoal).

---

## 🚀 Como Executar o Projeto

### 💻 Opção 1: Aplicativo Desktop Nativo para Windows (WPF — Sem Navegador!)
O projeto conta com uma aplicação Desktop nativa em **.NET WPF (`VibeMovies.Desktop`)** que consome diretamente as camadas de Clean Architecture (`Domain`, `Application` e `Infrastructure`), sem necessidade de abrir navegador ou subir servidor web separado:

* Basta dar dois cliques em **`Executar-VibeMovies-Desktop.bat`** na raiz do projeto, ou executar:
```bash
dotnet run --project backend/src/VibeMovies.Desktop/VibeMovies.Desktop.csproj
```

---

### 🌐 Opção 2: Aplicação Web (API + React SPA) — tudo em `http://localhost:5150`

O jeito mais simples: dois cliques em **`Executar-VibeMovies-Web.bat`** na raiz. Ele gera o build do frontend (`npm install` + `npm run build`) se `frontend/dist` não existir, compila a API em Release, sobe em `http://localhost:5150` e abre o navegador. A própria API serve a SPA (rotas que não são `/api` devolvem o `index.html`) e o Swagger continua em `/swagger`.

Manualmente:
```bash
cd frontend && npm install && npm run build && cd ..
dotnet run --project backend/src/VibeMovies.Api --urls http://localhost:5150
```
> A pasta do build é procurada em `Frontend:DistPath` (config) ou em `../../../frontend/dist` a partir do projeto da API. Mesmo rodando `dotnet VibeMovies.Api.dll` sem `--urls`, a API escuta em `http://localhost:5150` (a não ser que `ASPNETCORE_URLS`/`--urls` digam outra coisa).

#### 1. Banco de Dados (PostgreSQL via Docker ou Local Zero-Config)
Para subir o PostgreSQL via Docker:
```bash
docker-compose up -d
```
> **Nota:** Caso o PostgreSQL não esteja rodando na porta `5432`, o backend detecta automaticamente e utiliza um banco local SQLite (`vibemovies.db`) com a mesma estrutura do Entity Framework Core para facilitar testes imediatos.

### 2. Executar o Backend (.NET API)
```bash
cd backend
dotnet run --project src/VibeMovies.Api
```
A API estará disponível em `http://localhost:5150` (com documentação Swagger em `http://localhost:5150/swagger`).

#### Configurando sua Chave da API do TMDB (Opcional)
Você pode informar sua API Key gratuita do TMDB de três formas:
1. Diretamente na interface web do **VibeMovies** clicando no botão **"Configurar TMDB API"** no topo da página.
2. No arquivo `backend/src/VibeMovies.Api/appsettings.json` em `Tmdb:ApiKey` ou `Tmdb:ReadAccessToken`.
3. Via variável de ambiente `TMDB__ApiKey`.
*(Mesmo sem chave configurada, o sistema conta com um catálogo rico pré-carregado com metadados reais do TMDB e provedores do Brasil para demonstração imediata!)*

### 3. Executar o Frontend (React + Vite)
```bash
cd frontend
npm install
npm run dev
```
Acesse no navegador em `http://localhost:5173`.

---

## ✨ Novidades da API

* **Me surpreenda** — sorteio ponderado (vibe + nota) com explicação em pt-BR e uma frase pronta para assistente de voz.
* **Filmes parecidos** — recomendações do TMDB para o filme; sem chave, ranqueia o catálogo local por vibes/gêneros em comum.
* **Trailer e tagline** nos detalhes (`trailerYoutubeKey` — trailer em pt-BR, senão em inglês — e `tagline`).
* **Perfil com "meus streamings"** (`PUT /api/auth/me`) e filtro `onlyMyProviders=true` no discover.
* **Para você** — recomendações a partir dos filmes avaliados com 4★ ou mais e do Quero Ver, sempre fora do que já está no Diário, cada uma com `recommendationReason`.
* **Estatísticas completas** — top gêneros, distribuição das notas (1 a 5), assistidos por mês (12 meses) e horas assistidas. Filmes sem duração conhecida **não** entram na soma.
* **Exportar o Diário** em CSV (separador `;` + BOM UTF-8, abre direto no Excel) ou JSON.
* **Filtro de streaming na origem** — `provider=Netflix,Max` vira `with_watch_providers=8|1899|384` + `watch_monetization_types=flatrate` no TMDB, antes da paginação.
* **Dados honestos** — sem provedores inventados (filme desconhecido → lista vazia + link do TMDB), `vibeMatchScore` calculado de verdade (gêneros das vibes, nota e votos) e Radar de Cinemas com `isSimulatedSchedule`/`notice`. Os horários são **ilustrativos**, a classificação indicativa vem do TMDB (BR) quando há chave e os links de ingresso abrem a busca do filme no Ingresso.com.
* **Segurança** — chave JWT fora do repositório (`Jwt__SecretKey` ou `jwt.key` gerado na primeira execução, ao lado do banco), expiração configurável, CORS restrito a `localhost`/`127.0.0.1` (+ `Cors:AllowedOrigins`), validação de entrada, erros em ProblemDetails com campo `message`, rate limit de 10 req/min por IP em login/registro e compressão de respostas.
* **Bancos antigos continuam funcionando** — um upgrader idempotente adiciona as colunas novas depois do `EnsureCreated` (SQLite e PostgreSQL).

### Endpoints

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/api/health` | — | `status`, `version`, `databaseProvider`, `usingLiveTmdb`, `frontendServed`, `timestamp` |
| GET | `/api/movies/vibes` · `/api/movies/genres` | — | Catálogos |
| GET | `/api/movies/discover?vibes=&genres=&maxRuntime=&decade=&minRating=&search=&provider=Netflix,Max&onlyMyProviders=true&page=` | opcional* | Descoberta por vibe (*`onlyMyProviders` exige login) |
| GET | `/api/movies/search?q=` | — | Busca textual |
| GET | `/api/movies/{id}` | — | Detalhes (+ `trailerYoutubeKey`, `tagline`) |
| GET | `/api/movies/{id}/providers` | — | Onde assistir |
| GET | `/api/movies/{id}/similar?region=BR` | — | Até 8 filmes parecidos |
| GET | `/api/movies/surprise?vibes=a,b&genres=&maxRuntime=&provider=&excludeIds=1,2` | opcional | `{ movie, reason, spokenText }` |
| GET | `/api/cinemas/cities` · `/api/cinemas/now-playing?city=` | — | Radar (+ `isSimulatedSchedule`, `notice`) |
| POST | `/api/auth/register` · `/api/auth/login` | — | Rate limit: 10/min por IP |
| GET/PUT | `/api/auth/me` | ✔ | Perfil `{ userId, name, email, preferredRegion, defaultCity, myProviders[] }` |
| GET/POST/PUT/DELETE | `/api/watchlist` | ✔ | Diário de Bordo |
| GET | `/api/watchlist/stats` | ✔ | Estatísticas completas |
| GET | `/api/watchlist/recommendations` | ✔ | "Para você" (12 filmes com `recommendationReason`) |
| GET | `/api/watchlist/export?format=csv\|json` | ✔ | Download do Diário |
| GET | `/api/jarvis/summary?city=` | só localhost | Resumo para o assistente Jarvis |
| GET | `/api/jarvis/surprise?vibe=terror&maxRuntime=120` | só localhost | Sugestão aceitando palavra livre em pt-BR |
| GET | `/api/jarvis/vibes` | só localhost | `[{ id, name, emoji }]` |
| POST | `/api/jarvis/watchlist` | só localhost | `{ tmdbId, list: "want"\|"watched", rating? }` |

Erros seguem o padrão ProblemDetails e sempre trazem `message` em pt-BR.

## 🤖 Integração com o Jarvis

As rotas `/api/jarvis/*` existem para o assistente pessoal Jarvis (rodando na mesma máquina) consultar e alimentar o VibeMovies **sem JWT**. Por isso só respondem a requisições vindas de `localhost` (loopback); de qualquer outro IP a resposta é **403**.

* **Usuário**: o do e-mail em `Jarvis:UserEmail` (padrão `demo@vibemovies.app`); se ele não existir, o primeiro usuário cadastrado.
* **`GET /api/jarvis/summary?city=Joinville/SC`** → `{ generatedAt, usingLiveTmdb, user: { name, defaultCity }, tonightPick, tonightReason, tonightSpokenText, wantToWatch: [{ tmdbId, title, posterUrl, releaseYear, runtimeMinutes, providers[] }], nowPlaying: [{ tmdbId, title, posterUrl, voteAverage, ageRating, nextSessions: [{ cinema, time, room, audio }] }], isSimulatedSchedule, stats }`.
  O `tonightPick` vem primeiro do Quero Ver: dá preferência a um filme disponível nos seus streamings e, se a lista estiver vazia, sorteia um filme do seu gênero favorito. A cidade padrão é a do perfil, e `nextSessions` lista só as próximas sessões do dia.
* **`GET /api/jarvis/surprise?vibe=terror&maxRuntime=120`** → `{ movie, reason, spokenText }`. O parâmetro `vibe` aceita o id de uma vibe (`plot-twist`) ou uma palavra comum em pt-BR (`terror`, `comédia`, `ação`, `suspense`, `romance`, `ficção`, `leve`, `vingança`…).
* **`GET /api/jarvis/vibes`** → `[{ id, name, emoji }]`.
* **`POST /api/jarvis/watchlist`** com `{ "tmdbId": 27205, "list": "want" | "watched", "rating": 5 }` → item salvo no Diário.

Sem chave do TMDB, todos esses endpoints respondem com dados do catálogo local.

## 🔐 Configuração

| Chave | Padrão | Uso |
|---|---|---|
| `Jwt__SecretKey` (env) / `Jwt:SecretKey` | — | Segredo JWT (≥ 32 bytes). Sem ela, é gerado um `jwt.key` aleatório ao lado do banco (ignorado no git) |
| `Jwt:ExpirationDays` | 14 | Validade do token |
| `Cors:AllowedOrigins` | `[]` | Origens extras, além de localhost/127.0.0.1 |
| `RateLimiting:AuthPermitLimit` | 10 | Tentativas por minuto por IP em login/registro |
| `Jarvis:UserEmail` | `demo@vibemovies.app` | Usuário usado pelas rotas do Jarvis |
| `Frontend:DistPath` | (auto) | Pasta do build da SPA |
| `Tmdb:ApiKey` / `TMDB__ApiKey` | — | Chave do TMDB (v3 ou token v4) |

### Testes
```bash
dotnet test backend/VibeMovies.sln
```

