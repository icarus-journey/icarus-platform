# Icarus Platform — Documentação do Projeto

> Documento vivo. Mantido e atualizado à medida que novas decisões e features forem implementadas.
> Última atualização: 2026-08-27

## 0. Regra permanente do projeto

**Sempre seguir o padrão oficial/convenções da comunidade .NET**, a menos que
haja uma justificativa explícita registrada aqui em contrário. Isso inclui
(mas não se limita a):
- Estrutura e nomenclatura de projetos/pastas consistentes com Clean Architecture.
- Estilo de código conforme `.editorconfig` (mesmas convenções usadas pelo
  runtime/SDK oficiais da Microsoft: PascalCase para tipos/membros, camelCase
  para locais/parâmetros, `_camelCase` para campos privados, `I` como prefixo
  de interface, chaves em linhas próprias, `var` quando o tipo é óbvio, etc).
- `Nullable`, `ImplicitUsings` e analisadores do .NET habilitados em todos os projetos.
- Gerenciamento centralizado de versões de pacotes (Central Package Management).
- Segredos/credenciais **nunca** em `appsettings.json` versionado — usar
  User Secrets em desenvolvimento e variáveis de ambiente/secret manager em
  outros ambientes.
- `global.json` fixando a versão do SDK para builds reprodutíveis.

Qualquer novo código, projeto ou configuração adicionada à solução deve
seguir essas convenções por padrão.

## 1. Visão geral

Icarus Platform é a base de uma solução .NET que concentra API, processamento
assíncrono, regras de negócio e persistência dos dados transacionais do Icarus
(descrição extraída do `README.md`).

Estado atual: **projeto em estágio inicial**. A estrutura de solução (Clean
Architecture) e a configuração de persistência com Entity Framework Core /
PostgreSQL já foram criadas, mas ainda não há entidades de domínio, casos de
uso, controllers ou migrations implementados. Não há nenhuma feature de
negócio codificada ainda.

## 2. Repositório e fluxo Git

- **Remote**: `https://github.com/icarus-journey/icarus-platform.git` (org `icarus-journey`)
- **Branches principais**: `main`, `homologation`, `development`
- **Fluxo de PR obrigatório** (validado via CI, ver seção 5):
  - `feature/*` → `development`
  - `development` → `homologation`
  - `homologation` → `main`
- Branch de trabalho atual: `development`

## 3. Stack técnica

| Camada | Tecnologia |
|---|---|
| Linguagem / Runtime | C# / .NET 10 (`net10.0`) |
| Tipo de aplicação | ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`) |
| Arquitetura | Clean Architecture (Api → Application → Infrastructure → Domain) |
| ORM | Entity Framework Core 10 |
| Banco de dados | PostgreSQL (via `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3) |
| Documentação de API | `Microsoft.AspNetCore.OpenApi` 10.0.11 (endpoint `/openapi`, ativo só em Development) |
| Testes | xUnit 2.9.3 + `Microsoft.NET.Test.Sdk` 17.14.1 + `coverlet.collector` (cobertura) |
| CI | GitHub Actions |
| Banco de dados (ambiente local) | PostgreSQL 16 via container Docker (Docker Desktop), orquestrado com `docker-compose.yml` |
| IDE | Visual Studio (pasta `.vs/` e `Icarus.slnx` — formato de solução novo do VS) |

Versão do SDK do .NET instalada no ambiente: `10.0.400`.

## 4. Estrutura da solução

Arquivo de solução: `Icarus.slnx` (novo formato `.slnx` do Visual Studio, substitui `.sln`).

```
icarus-platform/
├── Icarus.slnx
├── global.json                  (fixa a versão do SDK .NET)
├── Directory.Build.props        (propriedades MSBuild comuns a todos os projetos)
├── Directory.Packages.props     (Central Package Management — versões de pacotes)
├── .editorconfig                (estilo de código, padrão comunidade .NET)
├── .gitattributes                (normalização de fim de linha)
├── docker-compose.yml           (PostgreSQL local)
├── .env.example
├── README.md
├── PROJETO.md
├── .github/workflows/validacao-inicial.yml
├── src/
│   ├── Icarus.Api/              (camada de apresentação — Web API)
│   ├── Icarus.Application/      (camada de aplicação — casos de uso)
│   ├── Icarus.Domain/           (camada de domínio — entidades/regras de negócio)
│   └── Icarus.Infrastructure/   (camada de infraestrutura — persistência, DI)
└── tests/
    └── Icarus.Api.Tests/        (testes da API)
```

### 4.0 Convenções de build/estilo (padrão comunidade .NET)

Para evitar duplicação e manter consistência entre os projetos, algumas
configurações foram centralizadas na raiz da solução em vez de repetidas em
cada `.csproj`:

- **`global.json`**: fixa a versão do SDK .NET (`10.0.400`, `rollForward: latestFeature`)
  usada para build/restore, garantindo builds reprodutíveis entre máquinas/CI.
- **`Directory.Build.props`**: define `TargetFramework`, `Nullable`,
  `ImplicitUsings`, `LangVersion`, e habilita os analisadores nativos do .NET
  (`EnableNETAnalyzers`, `AnalysisLevel=latest`, `AnalysisMode=Recommended`,
  `EnforceCodeStyleInBuild`) para **todos** os projetos automaticamente —
  os `.csproj` individuais não precisam mais declarar essas propriedades.
- **`Directory.Packages.props`**: habilita *Central Package Management*
  (`ManagePackageVersionsCentrally`). As versões dos pacotes NuGet ficam
  únicas nesse arquivo; cada `.csproj` referencia o pacote sem `Version`.
- **`.editorconfig`**: define o estilo de código C# seguindo o padrão adotado
  pelo runtime/SDK oficiais da Microsoft (nomenclatura, `var`, membros
  expression-bodied, namespaces com file-scoped, chaves em linha própria,
  etc). É respeitado pelo Visual Studio, Rider e `dotnet format`.
- **`.gitattributes`**: normaliza fim de linha (`lf`) para arquivos de texto,
  evitando diffs espúrios entre Windows/Linux (dev é Windows, CI é Ubuntu).

### 4.1 Grafo de dependências entre projetos

```
Icarus.Api  ──> Icarus.Application ──> Icarus.Domain
Icarus.Api  ──> Icarus.Infrastructure ──> Icarus.Application ──> Icarus.Domain
Icarus.Api.Tests ──> Icarus.Api
```

- `Icarus.Domain`: sem dependências (camada mais interna), ainda sem nenhuma classe própria.
- `Icarus.Application`: referencia `Icarus.Domain`. Ainda vazio (sem casos de uso).
- `Icarus.Infrastructure`: referencia `Icarus.Application`. Contém `DependencyInjection.cs` e o `DbContext`.
- `Icarus.Api`: referencia `Icarus.Application` e `Icarus.Infrastructure`. Ponto de entrada (`Program.cs`).
- `Icarus.Api.Tests`: referencia `Icarus.Api`. Contém 1 teste vazio de exemplo (`TesteExemplo.cs`).

### 4.2 Detalhe por projeto

**`src/Icarus.Api`** (`Icarus.Api.csproj`)
- SDK: `Microsoft.NET.Sdk.Web` (`TargetFramework`/`Nullable`/`ImplicitUsings` herdados do `Directory.Build.props`).
- `UserSecretsId` configurado (User Secrets habilitado — ver credenciais abaixo).
- Pacotes (versão centralizada em `Directory.Packages.props`): `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design` (para suportar `dotnet ef migrations`).
- `Program.cs`: configura `AddControllers()`, `AddOpenApi()`, chama `AddInfrastructure(builder.Configuration)`, expõe `/openapi` apenas em Development, `UseHttpsRedirection`, `UseAuthorization`, `MapControllers`.
- Não há nenhum Controller implementado ainda (só o template `Icarus.Api.http` referencia `/weatherforecast/`, que não existe mais no código — arquivo de exemplo desatualizado/residual).
- `appsettings.json`: **não contém mais credenciais**. `ConnectionStrings:DefaultConnection` foi movida para User Secrets (ver seção 6.1).
- `launchSettings.json`: perfis `http` (porta 5138) e `https` (portas 7220/5138).

**`src/Icarus.Application`** (`Icarus.Application.csproj`)
- SDK: `Microsoft.NET.Sdk`.
- Referencia `Icarus.Domain`.
- Sem nenhuma classe implementada ainda (pasta só tem o `.csproj`).

**`src/Icarus.Domain`** (`Icarus.Domain.csproj`)
- SDK: `Microsoft.NET.Sdk`.
- Sem dependências, sem nenhuma classe implementada ainda.

**`src/Icarus.Infrastructure`** (`Icarus.Infrastructure.csproj`)
- SDK: `Microsoft.NET.Sdk`.
- Referencia `Icarus.Application`.
- Pacote (versão centralizada): `Npgsql.EntityFrameworkCore.PostgreSQL`.
- `DependencyInjection.cs`: método de extensão `AddInfrastructure(IServiceCollection, IConfiguration)` que lê `ConnectionStrings:DefaultConnection` e registra `IcarusDbContext` via `UseNpgsql`. Lança `InvalidOperationException` se a connection string não existir.
- `Persistence/IcarusDbContext.cs`: `DbContext` principal, aplica configurações de entidades via `ApplyConfigurationsFromAssembly` (convenção para `IEntityTypeConfiguration<T>`). Ainda sem nenhum `DbSet` nem entidades mapeadas.
- Não há migrations criadas ainda (nenhuma pasta `Migrations/`).

**`tests/Icarus.Api.Tests`** (`Icarus.Api.Tests.csproj`)
- Framework: xUnit (versões centralizadas em `Directory.Packages.props`): `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`.
- Único teste: `TesteExemplo.Teste1` — vazio, apenas placeholder, não testa nada ainda.

## 5. CI/CD

**Workflow**: `.github/workflows/validacao-inicial.yml` ("Validação inicial")

- Disparado em `push` e `pull_request` para `development`, `homologation`, `main`.
- Job único `validar` (ubuntu-latest):
  1. Checkout (`actions/checkout@v7.0.1`).
  2. Verifica existência de `README.md` e `.gitignore`.
  3. Em PRs, valida a direção do fluxo entre branches:
     - destino `main` exige origem `homologation`
     - destino `homologation` exige origem `development`
     - destino `development` exige origem `feature/*`
  4. Verifica espaços em branco (`git diff-tree --check`).
- **Não há** ainda build (`dotnet build`), testes (`dotnet test`) ou lint automatizados no pipeline — só validação estrutural/formatação.

## 6. Como rodar localmente

### 6.1 Banco de dados (PostgreSQL via Docker)

O PostgreSQL **não é instalado na máquina** — roda como container via Docker
Desktop, definido em `docker-compose.yml` na raiz do repositório.

- `docker-compose.yml`: sobe um serviço `postgres` (imagem `postgres:16`),
  expõe a porta `5432` no host, persiste os dados em um volume nomeado
  (`icarus-postgres-data`) e tem healthcheck via `pg_isready`.
- Credenciais/parâmetros vêm de variáveis de ambiente com defaults
  (`postgres`/`postgres`/`icarus`/`5432`), lidas de um arquivo `.env` local
  (não versionado — já coberto pelo `.gitignore`). Existe um `.env.example`
  versionado como referência dos valores esperados.

Primeira vez / setup:
```bash
cp .env.example .env
docker compose up -d
```

Comandos úteis:
```bash
docker compose ps            # ver status/healthcheck do container
docker compose logs -f postgres
docker compose down          # para o container (mantém o volume/dados)
docker compose down -v       # para e apaga também os dados
```

A connection string **não fica mais no `appsettings.json`** (padrão .NET:
segredos não são versionados). Ela é armazenada via **User Secrets**
(`dotnet user-secrets`), lida automaticamente pelo ASP.NET Core quando
`ASPNETCORE_ENVIRONMENT=Development`. Setup em uma máquina nova:

```bash
cd src/Icarus.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=icarus;Username=postgres;Password=postgres"
```

Os valores acima já batem com os defaults do `.env.example` do container —
ou seja, com o container no ar e o secret configurado, a API conecta sem
nenhuma configuração adicional. Em outros ambientes (homologação/produção), a
connection string deve vir de variável de ambiente ou de um gerenciador de
segredos do provedor de nuvem — nunca de arquivo versionado.

Validado em 2026-08-27: com o container saudável e o secret configurado,
`dotnet ef dbcontext info` (rodado em `src/Icarus.Api`) confirmou conexão
bem-sucedida (`Data source: tcp://localhost:5432`, `Database name: icarus`).

### 6.2 API

```bash
dotnet restore
dotnet build
dotnet run --project src/Icarus.Api
```

- URLs padrão: `http://localhost:5138` (http) e `https://localhost:7220` (https).
- Testes: `dotnet test`

## 7. Lacunas conhecidas / próximos passos naturais

- Nenhuma entidade de domínio definida em `Icarus.Domain`.
- Nenhum caso de uso/serviço em `Icarus.Application`.
- Nenhum Controller em `Icarus.Api` (arquivo `.http` residual referencia endpoint inexistente).
- Nenhuma migration do EF Core criada.
- CI não roda build/test do .NET, apenas validações estruturais (nem `dotnet format`/analisadores, apesar do `.editorconfig` já estar configurado).
- Sem autenticação/autorização configurada (só `UseAuthorization()` chamado, sem esquema definido).

## 8. Histórico de decisões e features (a atualizar conforme avançarmos)

> Esta seção será atualizada a cada nova instrução/feature implementada.

- 2026-08-26 — Mapeamento inicial do projeto (este documento).
- 2026-08-26 — PostgreSQL passou a rodar em container Docker (Docker Desktop)
  em vez de SGBD instalado na máquina. Adicionados `docker-compose.yml` (serviço
  `postgres:16`, volume nomeado, healthcheck) e `.env.example`. Conexão validada
  via `dotnet ef dbcontext info`.
- 2026-08-27 — Solução alinhada ao padrão da comunidade .NET: adicionados
  `global.json` (SDK fixo), `Directory.Build.props` (propriedades comuns +
  analisadores .NET), `Directory.Packages.props` (Central Package Management),
  `.editorconfig` (estilo de código) e `.gitattributes` (normalização de EOL).
  `ConnectionStrings:DefaultConnection` removida do `appsettings.json`
  versionado e movida para User Secrets (`UserSecretsId` adicionado ao
  `Icarus.Api.csproj`). Build e testes (`dotnet build`, `dotnet test`)
  validados após as mudanças; conexão ao Postgres revalidada via
  `dotnet ef dbcontext info`. Regra permanente registrada na seção 0: sempre
  seguir o padrão .NET da comunidade.
