# Local-First Enterprise RAG — Implementation Plan

## Project Overview

A production-ready, air-gapped Retrieval-Augmented Generation system using SQL Server 2025 native vector search, Ollama local LLM, and .NET 9 / WPF. Built as a portfolio piece to demonstrate enterprise AI data architecture for regulated industries (Finance, Healthcare, Defense). Deployed to Azure to prove cloud scalability.

**Philosophy:** Hybrid Portability — develop local-first (air-gapped), deploy to Azure (enterprise-scale).

---

## Technology Stack

| Layer          | Technology                         | Version / Model            |
|----------------|------------------------------------|----------------------------|
| Database       | SQL Server 2025 (Preview)          | Docker: `mcr.microsoft.com/mssql/server:2025-latest` |
| Vector Search  | Native `VECTOR(1024)` + DiskANN    | Built into SQL Server 2025 |
| LLM Generation | Ollama                             | `llama3.2`                 |
| Embeddings     | Ollama                             | `mxbai-embed-large` (1024-dim) |
| Orchestration  | .NET 9, C#, Microsoft Semantic Kernel | `Microsoft.SemanticKernel` NuGet |
| Frontend       | WPF (Windows Desktop)              | .NET 9 `net9.0-windows`   |
| API Gateway    | Nginx                              | Reverse proxy for HTTPS    |
| Containers     | Docker Desktop                     | `docker-compose.yml`       |
| CI/CD          | GitHub Actions                     | Deploy to Azure on push    |
| Cloud          | Azure (SQL MI, Container Apps, App Service) | Phase B |

---

## Repository Structure

```
/
├── CLAUDE.md                          # This file — implementation guide
├── README.md                          # Portfolio-facing project overview
├── .gitignore
├── docker-compose.yml                 # Local dev: SQL Server 2025 + Ollama + Nginx
├── docker-compose.override.yml        # Local dev overrides (ports, volumes)
│
├── docs/
│   └── architecture.md                # Architecture decision records
│
├── infrastructure/
│   ├── docker/
│   │   ├── sql-server/
│   │   │   └── Dockerfile             # SQL Server 2025 with AdventureWorksDW2020 pre-loaded
│   │   ├── ollama/
│   │   │   └── Dockerfile             # Ollama with pre-pulled models
│   │   └── nginx/
│   │       ├── Dockerfile
│   │       └── nginx.conf             # HTTPS reverse proxy config
│   ├── azure/
│   │   ├── main.bicep                 # Azure infrastructure-as-code
│   │   ├── parameters.json
│   │   └── deploy.sh                  # One-command Azure deployment
│   └── scripts/
│       ├── init-db.sh                 # Wait for SQL, restore AdventureWorksDW2020, apply schema
│       └── pull-models.sh             # Pull Ollama models on first run
│
├── sql/
│   ├── 001-enable-external-endpoints.sql    # Enable REST endpoint configuration
│   ├── 002-alter-dimproduct-vector.sql      # ALTER TABLE add VECTOR(1024) column
│   ├── 003-create-hybrid-search-sproc.sql   # Hybrid search stored procedure
│   └── 004-create-vector-index.sql          # DiskANN vector index
│
├── src/
│   ├── EnterpriseRag.sln                    # Solution file
│   │
│   ├── EnterpriseRag.Core/                  # Class library — shared models & interfaces
│   │   ├── EnterpriseRag.Core.csproj
│   │   ├── Models/
│   │   │   ├── Product.cs                   # DimProduct domain model
│   │   │   ├── SearchRequest.cs             # Query + optional metadata filters
│   │   │   ├── SearchResult.cs              # Ranked product + similarity score
│   │   │   └── RagResponse.cs               # Final LLM-generated answer + sources
│   │   ├── Interfaces/
│   │   │   ├── IEmbeddingClient.cs          # Generate embeddings from text
│   │   │   ├── ISearchService.cs            # Execute hybrid vector search
│   │   │   ├── IGenerationClient.cs         # Send prompt to LLM, get response
│   │   │   └── IRagOrchestrator.cs          # Full RAG pipeline coordination
│   │   └── Configuration/
│   │       ├── OllamaSettings.cs            # BaseUrl, EmbeddingModel, GenerationModel
│   │       ├── DatabaseSettings.cs          # ConnectionString, Schema, TopK
│   │       └── SearchSettings.cs            # DefaultTopK, SimilarityThreshold
│   │
│   ├── EnterpriseRag.Infrastructure/        # Class library — external service implementations
│   │   ├── EnterpriseRag.Infrastructure.csproj
│   │   ├── Ollama/
│   │   │   ├── OllamaEmbeddingClient.cs     # IEmbeddingClient → Ollama /api/embed
│   │   │   └── OllamaGenerationClient.cs    # IGenerationClient → Ollama /api/generate
│   │   ├── SqlServer/
│   │   │   ├── ProductRepository.cs         # ADO.NET queries against DimProduct
│   │   │   └── HybridSearchService.cs       # ISearchService → exec hybrid search sproc
│   │   ├── Rag/
│   │   │   └── RagOrchestrator.cs           # IRagOrchestrator — wires embed→search→generate
│   │   └── DependencyInjection.cs           # IServiceCollection extension for registration
│   │
│   ├── EnterpriseRag.Ingestion/             # Console app — batch embedding pipeline
│   │   ├── EnterpriseRag.Ingestion.csproj
│   │   ├── Program.cs                       # Host builder, runs IngestionService
│   │   └── IngestionService.cs              # Read products → embed → bulk UPDATE vectors
│   │
│   └── EnterpriseRag.Desktop/               # WPF application — user-facing frontend
│       ├── EnterpriseRag.Desktop.csproj
│       ├── App.xaml / App.xaml.cs            # DI container setup, theme
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── ViewModels/
│       │   ├── MainViewModel.cs             # Search input, results binding, RAG trigger
│       │   └── ViewModelBase.cs             # INotifyPropertyChanged base
│       ├── Views/
│       │   ├── SearchView.xaml              # Search box + filter dropdowns
│       │   └── ResultsView.xaml             # Markdown-rendered answer + source cards
│       ├── Converters/
│       │   └── MarkdownToFlowDocumentConverter.cs
│       └── Resources/
│           └── Styles.xaml                  # Consistent dark/light theme
│
├── tests/
│   ├── EnterpriseRag.Tests.Unit/            # xUnit — mocked unit tests
│   │   ├── EnterpriseRag.Tests.Unit.csproj
│   │   ├── OllamaEmbeddingClientTests.cs
│   │   ├── HybridSearchServiceTests.cs
│   │   └── RagOrchestratorTests.cs
│   └── EnterpriseRag.Tests.Integration/     # xUnit — requires Docker services running
│       ├── EnterpriseRag.Tests.Integration.csproj
│       ├── SqlServerVectorTests.cs          # Verify VECTOR_DISTANCE works end-to-end
│       └── OllamaApiTests.cs               # Verify embedding + generation against real Ollama
│
├── .github/
│   └── workflows/
│       ├── ci.yml                           # Build + unit tests on every PR
│       └── deploy.yml                       # Deploy to Azure on push to main
│
├── Polished Requirements/
│   └── index.html                           # Project requirements & strategy (complete)
│
└── Rough Requirements/                      # Original unformatted requirements (reference only)
    ├── Technical Requirements.txt
    ├── Strategic Marketing.txt
    └── Deployment and Hosting Strategy.txt
```

---

## Implementation Phases

### Phase 1: Infrastructure & Database (Start Here)

**Goal:** Docker environment running SQL Server 2025 + Ollama + Nginx, with schema applied.

#### 1.1 Docker Compose

Create `docker-compose.yml` with three services:

- **sql-server**: SQL Server 2025 container with SA password via environment variable. Mount volume for data persistence. Expose port 1433 locally.
- **ollama**: Ollama container. Mount volume for model cache. Expose port 11434 locally. Entrypoint pulls `llama3.2` and `mxbai-embed-large` on first boot.
- **nginx**: Nginx container with custom `nginx.conf` that reverse-proxies HTTPS (self-signed cert) to the Ollama HTTP endpoint. SQL Server 2025 external REST endpoints require HTTPS.

Health checks on all three services. The app should not start until all are healthy.

#### 1.2 Database Setup

Restore `AdventureWorksDW2020` backup into the SQL Server container. Then apply SQL scripts in order:

1. **`001-enable-external-endpoints.sql`** — `sp_configure 'external rest endpoint enabled', 1; RECONFIGURE;`
2. **`002-alter-dimproduct-vector.sql`** — `ALTER TABLE DimProduct ADD DescriptionVector VECTOR(1024) NULL;`
3. **`003-create-hybrid-search-sproc.sql`** — Stored procedure accepting `@QueryVector VECTOR(1024)`, optional `@Category NVARCHAR(50)`, optional `@Color NVARCHAR(15)`, `@TopK INT = 5`. Uses `VECTOR_DISTANCE('cosine', DescriptionVector, @QueryVector)` combined with `WHERE` clause metadata filters. Returns `ProductKey, EnglishProductName, EnglishDescription, Category, Color, SimilarityScore`.
4. **`004-create-vector-index.sql`** — Create a DiskANN vector index on `DescriptionVector` for fast approximate nearest-neighbor lookup.

#### 1.3 Verification

- `docker compose up -d` brings all services to healthy state
- Can connect to SQL Server on `localhost,1433` and query `DimProduct`
- `curl http://localhost:11434/api/tags` returns available Ollama models
- Nginx proxies `https://localhost:8443` to Ollama

---

### Phase 2: .NET Solution & Core Library

**Goal:** Solution scaffold with domain models, interfaces, and configuration.

#### 2.1 Create Solution

```bash
dotnet new sln -n EnterpriseRag -o src
dotnet new classlib -n EnterpriseRag.Core -o src/EnterpriseRag.Core -f net9.0
dotnet new classlib -n EnterpriseRag.Infrastructure -o src/EnterpriseRag.Infrastructure -f net9.0
dotnet new console -n EnterpriseRag.Ingestion -o src/EnterpriseRag.Ingestion -f net9.0
dotnet new wpf -n EnterpriseRag.Desktop -o src/EnterpriseRag.Desktop -f net9.0-windows
dotnet new xunit -n EnterpriseRag.Tests.Unit -o tests/EnterpriseRag.Tests.Unit -f net9.0
dotnet new xunit -n EnterpriseRag.Tests.Integration -o tests/EnterpriseRag.Tests.Integration -f net9.0
```

Add all projects to the solution. Set project references:
- `Infrastructure` → references `Core`
- `Ingestion` → references `Core`, `Infrastructure`
- `Desktop` → references `Core`, `Infrastructure`
- `Tests.Unit` → references `Core`, `Infrastructure`
- `Tests.Integration` → references `Core`, `Infrastructure`

#### 2.2 NuGet Packages

| Project          | Packages                                                                 |
|------------------|--------------------------------------------------------------------------|
| Core             | `Microsoft.SemanticKernel` (if needed for abstractions)                  |
| Infrastructure   | `Microsoft.Data.SqlClient`, `System.Text.Json`                           |
| Ingestion        | `Microsoft.Extensions.Hosting`                                           |
| Desktop          | `CommunityToolkit.Mvvm`, `Markdig.Wpf` (or similar markdown renderer)   |
| Tests.Unit       | `xunit`, `Moq`, `FluentAssertions`                                      |
| Tests.Integration| `xunit`, `FluentAssertions`, `Testcontainers` (optional)                 |

#### 2.3 Core Models & Interfaces

Implement all types in `EnterpriseRag.Core/Models/` and `EnterpriseRag.Core/Interfaces/` as specified in the repository structure. Keep models as simple POCOs. Interfaces should be minimal:

```csharp
public interface IEmbeddingClient
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> HybridSearchAsync(SearchRequest request, CancellationToken ct = default);
}

public interface IGenerationClient
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken ct = default);
}

public interface IRagOrchestrator
{
    Task<RagResponse> AskAsync(string question, string? category = null, string? color = null, CancellationToken ct = default);
}
```

#### 2.4 Configuration Classes

Bind to `appsettings.json` sections:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "EmbeddingModel": "mxbai-embed-large",
    "GenerationModel": "llama3.2"
  },
  "Database": {
    "ConnectionString": "Server=localhost,1433;Database=AdventureWorksDW2020;User Id=sa;Password=...;TrustServerCertificate=true;",
    "TopK": 5
  },
  "Search": {
    "DefaultTopK": 5,
    "SimilarityThreshold": 0.3
  }
}
```

---

### Phase 3: Infrastructure Implementations

**Goal:** Working Ollama client and SQL Server search service.

#### 3.1 OllamaEmbeddingClient

- Inject `HttpClient` (use `IHttpClientFactory`)
- POST to `{BaseUrl}/api/embed` with `{ "model": "mxbai-embed-large", "input": "text" }`
- Deserialize response `embedding` field as `float[]`
- Handle errors: model not loaded, Ollama unreachable

#### 3.2 OllamaGenerationClient

- POST to `{BaseUrl}/api/generate` with `{ "model": "llama3.2", "prompt": "...", "stream": false }`
- For streaming: set `"stream": true`, read NDJSON line-by-line, yield each `response` token
- The WPF app will use streaming for real-time display

#### 3.3 HybridSearchService

- Use `Microsoft.Data.SqlClient` with parameterized `SqlCommand`
- Call the `usp_HybridProductSearch` stored procedure
- Map `@QueryVector` parameter: convert `float[]` to the SQL Server vector format string `[0.123,0.456,...]`
- Map optional `@Category`, `@Color`, `@TopK` parameters
- Return `List<SearchResult>` ordered by similarity score descending

#### 3.4 RagOrchestrator

This is the central pipeline:

```
AskAsync(question, category?, color?)
  1. embedding = await _embeddingClient.GetEmbeddingAsync(question)
  2. results  = await _searchService.HybridSearchAsync(new SearchRequest(embedding, category, color))
  3. prompt   = BuildPrompt(question, results)   // System prompt + product context + user question
  4. answer   = await _generationClient.GenerateAsync(prompt)
  5. return new RagResponse(answer, results)
```

**Prompt template:**
```
You are a knowledgeable product advisor for AdventureWorks. Answer the user's
question using ONLY the product information provided below. If the information
is insufficient, say so. Be concise and helpful.

## Relevant Products
{foreach result: "- **{Name}** ({Category}, {Color}): {Description} [Score: {Score:F3}]"}

## User Question
{question}
```

#### 3.5 DependencyInjection.cs

Extension method `AddInfrastructure(this IServiceCollection services, IConfiguration config)` that registers:
- `HttpClient` for Ollama (named client with `BaseUrl`)
- `IEmbeddingClient` → `OllamaEmbeddingClient` (singleton)
- `IGenerationClient` → `OllamaGenerationClient` (singleton)
- `ISearchService` → `HybridSearchService` (scoped)
- `IRagOrchestrator` → `RagOrchestrator` (scoped)
- Bind `OllamaSettings`, `DatabaseSettings`, `SearchSettings` from `IConfiguration`

---

### Phase 4: Ingestion Pipeline

**Goal:** Console app that populates all DescriptionVector values.

#### 4.1 IngestionService

- Hosted service (`IHostedService`) that runs once and exits
- Query all `DimProduct` rows where `DescriptionVector IS NULL` and `EnglishDescription IS NOT NULL`
- For each product, call `IEmbeddingClient.GetEmbeddingAsync(product.EnglishDescription)`
- Batch UPDATE vectors using parameterized SQL (batch size: 50 rows per round-trip)
- Log progress: `Embedded {count}/{total} products`
- Idempotent — safe to re-run; only processes rows with NULL vectors

#### 4.2 Running

```bash
cd src/EnterpriseRag.Ingestion
dotnet run
# Output: Embedded 606/606 products in 2m 14s
```

---

### Phase 5: WPF Desktop Application

**Goal:** Polished desktop UI that demonstrates the full RAG loop.

#### 5.1 App Architecture (MVVM)

- **App.xaml.cs**: Build `IHost` with DI, register `MainViewModel`, call `AddInfrastructure()`
- **MainViewModel**:
  - Properties: `SearchQuery`, `SelectedCategory`, `SelectedColor`, `Answer`, `Sources`, `IsSearching`, `StatusMessage`
  - Commands: `SearchCommand` (async relay command)
  - On search: set `IsSearching=true`, call `IRagOrchestrator.AskAsync()`, bind results

#### 5.2 UI Layout (MainWindow.xaml)

```
┌──────────────────────────────────────────────────┐
│  Local-First Enterprise RAG          [Status: ●] │
├──────────────────────────────────────────────────┤
│  [Search Box________________________] [Search]   │
│  Category: [All ▾]   Color: [All ▾]             │
├──────────────────────────────────────────────────┤
│                                                  │
│  ┌─ AI Response ───────────────────────────────┐ │
│  │  (Markdown-rendered LLM answer)             │ │
│  │                                             │ │
│  └─────────────────────────────────────────────┘ │
│                                                  │
│  ┌─ Sources ───────────────────────────────────┐ │
│  │  1. Mountain-100 Black (0.892)              │ │
│  │  2. Road-150 Red (0.856)                    │ │
│  │  3. ...                                     │ │
│  └─────────────────────────────────────────────┘ │
│                                                  │
│  ── Status Bar: "Retrieved 5 products in 340ms" ─│
└──────────────────────────────────────────────────┘
```

#### 5.3 Key Features

- **Streaming response**: Use `IGenerationClient.GenerateStreamAsync()` to display tokens in real-time
- **Filter dropdowns**: Populated from distinct Category/Color values in DimProduct
- **Status indicator**: Green dot when all services (SQL, Ollama) are reachable; red when not
- **Performance metrics**: Display query time (embed + search + generate) in status bar
- **Error handling**: Graceful messages when Ollama or SQL Server is unreachable

#### 5.4 Styling

Use a clean, professional theme (dark sidebar optional). Consistent with the portfolio branding. Use `Styles.xaml` resource dictionary for reusable styles. Target a modern Windows 11 look.

---

### Phase 6: Testing

#### 6.1 Unit Tests (EnterpriseRag.Tests.Unit)

- **OllamaEmbeddingClientTests**: Mock `HttpMessageHandler`, verify correct API call format, deserialize response
- **HybridSearchServiceTests**: Mock `SqlConnection` (or use wrapper interface), verify sproc params
- **RagOrchestratorTests**: Mock all three interfaces, verify pipeline wiring and prompt construction

#### 6.2 Integration Tests (EnterpriseRag.Tests.Integration)

- **SqlServerVectorTests**: Requires Docker running. Insert a test row with a known vector, call hybrid search, verify it ranks highest
- **OllamaApiTests**: Requires Ollama running. Embed a string, verify 1024-dim float array returned. Generate a short completion, verify non-empty response

Run with: `dotnet test` (unit tests always; integration tests require `docker compose up`)

---

### Phase 7: Azure Deployment (Phase B)

**Goal:** Prove the same codebase scales to cloud infrastructure.

#### 7.1 Azure Infrastructure (Bicep)

`infrastructure/azure/main.bicep` provisions:
- **Azure SQL Managed Instance** (SQL 2025 tier) with VECTOR support
- **Azure Container Apps** (GPU SKU) running Ollama with `llama3.2` + `mxbai-embed-large`
- **Azure App Service** (.NET 9) hosting the backend (adapted from WPF to ASP.NET Minimal API for web access)
- **Azure Container Apps sidecar** for Nginx HTTPS proxy
- **Managed Identity** assigned to App Service, granted SQL db_datareader/db_datawriter
- **Virtual Network** isolating Ollama container (not internet-accessible)

#### 7.2 Connection String Strategy

| Environment | Auth Method                        |
|-------------|------------------------------------|
| Local       | SQL auth (sa password in Docker)   |
| Azure       | Managed Identity (no password)     |

The `DatabaseSettings.ConnectionString` uses `Authentication=Active Directory Managed Identity;` in Azure, detected via environment variable `AZURE_ENVIRONMENT=true`.

#### 7.3 GitHub Actions CI/CD

**`.github/workflows/ci.yml`** (on PR):
- Checkout → Setup .NET 9 → Restore → Build → Run unit tests

**`.github/workflows/deploy.yml`** (on push to main):
- Checkout → Setup .NET 9 → Build → Publish → Login to Azure → Deploy to App Service → Run SQL migrations via `sqlcmd`

#### 7.4 Web Adaptation

For the Azure deployment, create a thin ASP.NET Minimal API project (`EnterpriseRag.Web`) that exposes:
- `POST /api/ask` — accepts `{ question, category?, color? }`, returns `RagResponse`
- `GET /api/health` — checks SQL + Ollama connectivity
- Static files: a simple HTML/JS frontend (or Blazor) for browser-based demo

This reuses 100% of the Core and Infrastructure libraries. The WPF app remains the local-first proof; the web app is the cloud proof.

---

## Coding Standards

- **Target Framework**: `net9.0` (libraries), `net9.0-windows` (WPF only)
- **Nullable reference types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled
- **Async/await**: All I/O operations must be async. Use `CancellationToken` throughout.
- **No secrets in code**: Connection strings and passwords go in `appsettings.Development.json` (gitignored) or Docker environment variables. Azure uses Managed Identity.
- **ADO.NET over EF**: Use raw `Microsoft.Data.SqlClient` for SQL Server access. This project showcases T-SQL expertise — an ORM would hide that.
- **Logging**: Use `ILogger<T>` from `Microsoft.Extensions.Logging`. Structured logging with Serilog sink optional.

---

## Build Order (Recommended Sequence)

```
Phase 1  →  Phase 2  →  Phase 3  →  Phase 4  →  Phase 5  →  Phase 6  →  Phase 7
Docker     Solution     Services    Ingestion    WPF UI      Tests       Azure
& SQL      scaffold     (Ollama,    (populate    (full       (unit +     deployment
schema     & models     SQL, RAG)   vectors)     MVVM app)   integration)
```

Each phase produces a working, testable increment. Do not skip phases.

---

## Key Decisions & Rationale

| Decision | Rationale |
|----------|-----------|
| ADO.NET over Entity Framework | Showcases T-SQL/sproc expertise to hiring managers |
| WPF over Blazor/web (local) | Emphasizes "local-first / on-prem" — not a browser app |
| Separate Ingestion console app | Keeps embedding pipeline decoupled from the UI; can run as scheduled job |
| float[] over Semantic Kernel memory | Direct control over vector format; avoids SK memory abstractions that hide SQL Server specifics |
| Nginx sidecar | SQL Server 2025 external endpoints require HTTPS; Nginx is lightweight and familiar to ops teams |
| DiskANN index | Microsoft Research algorithm; key portfolio talking point for cost vs. Pinecone |
| Managed Identity in Azure | Zero-trust posture; no connection strings in code; green flag for security reviewers |
