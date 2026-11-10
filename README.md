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

### 1. Banco de Dados (PostgreSQL via Docker ou Local Zero-Config)
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
