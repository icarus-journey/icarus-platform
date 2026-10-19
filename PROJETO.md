# Icarus Platform — Documentação do Projeto

> Documento vivo. Mantido e atualizado à medida que novas decisões e features forem implementadas.
> Última atualização: 2026-10-17 (onboarding por IA de ponta a ponta: RabbitMQ.Client, `TrabalhoOnboarding`, Épico/Campanha/Missão com dificuldade/pontuação)

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

**Exceção deliberada e registrada:** entidades de domínio e campos de tabela
são nomeados **em português**, inclusive campos técnicos como `CriadoEm`/
`AtualizadoEm` (nunca `CreatedAt`/`UpdatedAt`) — para casar 1:1 com a
linguagem ubíqua do negócio e os documentos do `icarus-context`. Decisão
tomada em 2026-08-29 (ver seção 4.3). Fora do modelo de dados (nomes de
projeto, classes de infraestrutura como `DependencyInjection`, métodos como
`AddInfrastructure`), o inglês continua sendo o padrão.

Qualquer novo código, projeto ou configuração adicionada à solução deve
seguir essas convenções por padrão.

## 1. Visão geral

Icarus Platform é a base de uma solução .NET que concentra API, processamento
assíncrono, regras de negócio e persistência dos dados transacionais do Icarus
(descrição extraída do `README.md`).

Estado atual: **projeto em estágio inicial**. A estrutura de solução (Clean
Architecture), a persistência com Entity Framework Core / PostgreSQL e o
modelo de dados completo do MVP (14 entidades, ver seção 4.3), incluindo
ocorrências persistidas para missões recorrentes, já foram criados.
Autenticação (cadastro/login) está em construção — ver seção 4.4. Endpoints
de negócio expostos hoje: cadastro (`POST /api/auth/cadastro`) e login
(`POST /api/auth/login`).

O modelo de dados segue o DER do `icarus-context` na versão **1.2**
(2026-09-08), incluindo ocorrências persistidas para missões recorrentes.

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
| Logging | Serilog (`Serilog.AspNetCore` 10.0.0), JSON estruturado no stdout e no Loki (`Serilog.Sinks.Grafana.Loki`) |
| Traces / Métricas | OpenTelemetry (`OpenTelemetry.Extensions.Hosting` 1.19.1), exportadas via OTLP para Tempo/Prometheus (`icarus-infrastructure`) |
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
    ├── Icarus.Application.UnitTests/    (testa Icarus.Application)
    ├── Icarus.Infrastructure.UnitTests/ (testa Icarus.Infrastructure)
    └── Icarus.Api.UnitTests/            (testa Icarus.Api)
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

Icarus.Application.UnitTests    ──> Icarus.Application
Icarus.Infrastructure.UnitTests ──> Icarus.Infrastructure
Icarus.Api.UnitTests            ──> Icarus.Api
```

- `Icarus.Domain`: sem dependências (camada mais interna). Contém as 14 entidades do modelo de dados e os enums (ver seção 4.3). Sem projeto de teste próprio por ora — ver seção 4.5 ("Por que não existe `Icarus.Domain.UnitTests`").
- `Icarus.Application`: referencia `Icarus.Domain`. Contém os contratos (`Interfaces/`), o caso de uso de cadastro (`UseCases/RegisterUser/`) e `Exceptions/` (ver seção 4.4).
- `Icarus.Infrastructure`: referencia `Icarus.Application`. Contém `DependencyInjection.cs`, o `DbContext`, as configurações EF Core e as migrations.
- `Icarus.Api`: referencia `Icarus.Application` e `Icarus.Infrastructure`. Ponto de entrada (`Program.cs`).
- Um projeto de teste por projeto de produção que **tem algo a testar** (`Icarus.Application.UnitTests`, `Icarus.Infrastructure.UnitTests`, `Icarus.Api.UnitTests`), cada um referenciando só a camada que testa — nunca a camada de teste de outro. Ver seção 4.5 para a convenção completa.

### 4.2 Detalhe por projeto

**`src/Icarus.Api`** (`Icarus.Api.csproj`)
- SDK: `Microsoft.NET.Sdk.Web` (`TargetFramework`/`Nullable`/`ImplicitUsings` herdados do `Directory.Build.props`).
- `UserSecretsId` configurado (User Secrets habilitado — ver credenciais abaixo).
- Pacotes (versão centralizada em `Directory.Packages.props`): `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design` (para suportar `dotnet ef migrations`).
- `Program.cs`: encadeia `AddApi()`, `AddApplication()` e `AddInfrastructure(builder.Configuration)`; pipeline com `UseExceptionHandler`, `UseStatusCodePages`, `/openapi` apenas em Development, `UseHttpsRedirection`, `UseAuthorization`, `MapControllers`.
- Pastas: `Controllers/` (`AuthController`), `Contracts/Auth/` (Request/Response e validador), `Filters/` (`ValidationFilter`), `ExceptionHandling/` (`GlobalExceptionHandler`). Detalhes na seção 4.4.
- Endpoints de negócio em `AuthController` (cadastro, login, refresh, `me`) — ver seção 4.4. Teste manual via Swagger UI (seção 4.4); o arquivo `.http` do template foi removido (decisão do usuário, 2026-10-10).
- Pacotes: `FluentValidation` (validação dos Requests).
- `appsettings.json`: **não contém mais credenciais**. `ConnectionStrings:DefaultConnection` foi movida para User Secrets (ver seção 6.1).
- `launchSettings.json`: perfis `http` (porta 5138) e `https` (portas 7220/5138).

**`src/Icarus.Application`** (`Icarus.Application.csproj`)
- SDK: `Microsoft.NET.Sdk`.
- Referencia `Icarus.Domain`.
- Pastas: `Interfaces/` (`IPasswordHasher`, `IUsuarioRepository`, `IUnitOfWork`), `UseCases/RegisterUser/`, `Exceptions/`; `DependencyInjection.cs` com `AddApplication()`. Pacote: `Microsoft.Extensions.DependencyInjection.Abstractions`.

**`src/Icarus.Domain`** (`Icarus.Domain.csproj`)
- SDK: `Microsoft.NET.Sdk`.
- Sem dependências (nem de EF Core) — entidades são POCOs puros, mapeamento fica todo em `Icarus.Infrastructure`.
- `Entities/`: as 14 entidades do modelo de dados (ver seção 4.3).
- `Common/EntidadeBase.cs`: classe abstrata com `CriadoEm`/`AtualizadoEm`, herdada por todas as entidades exceto `MovimentacaoPontos` (que só tem `CriadoEm`, por ser um registro imutável). Fica em `Common/` (não em `Entities/`) seguindo o padrão dos templates de Clean Architecture .NET mais adotados pela comunidade (Jason Taylor, Ardalis): abstrações compartilhadas do domínio (bases, futuras interfaces de evento de domínio etc.) ficam separadas das entidades concretas.
- `Enums/`: `StatusMissao`, `StatusOcorrencia`, `EstadoEpico`, `EstadoCampanha`, `StatusAprovacaoMissaoIA`, `ControlaApp`, `TipoRelatorio`, `TipoMovimentacaoPontos` — listas iniciais sugeridas pelo DER, ainda decisões de domínio em aberto.

**`src/Icarus.Infrastructure`** (`Icarus.Infrastructure.csproj`)
- SDK: `Microsoft.NET.Sdk`.
- Referencia `Icarus.Application` (e, transitivamente, `Icarus.Domain`).
- Pacotes (versão centralizada): `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`.
- `DependencyInjection.cs`: método de extensão `AddInfrastructure(IServiceCollection, IConfiguration)` que lê `ConnectionStrings:DefaultConnection`, registra `IcarusDbContext` via `UseNpgsql` e habilita `UseSnakeCaseNamingConvention()` (ver seção 4.3). Lança `InvalidOperationException` se a connection string não existir.
- `Persistence/IcarusDbContext.cs`: `DbContext` principal. Expõe um `DbSet<T>` por entidade, aplica configurações via `ApplyConfigurationsFromAssembly` e sobrescreve `SaveChanges`/`SaveChangesAsync` para preencher `CriadoEm`/`AtualizadoEm` automaticamente (não fica a cargo de quem chama o repositório).
- `Persistence/Configurations/`: uma classe `IEntityTypeConfiguration<T>` por entidade (Fluent API — chaves, FKs, índices, `CHECK` constraints, `UNIQUE`, tipos de coluna).
- `Persistence/Migrations/`: migration inicial `CriarModeloInicial` (ver seção 4.3), já aplicada ao banco local.

### 4.3 Modelo de dados (PostgreSQL)

Fonte: [`docs/implementation-design/platform/02-modelos-de-dados-postgresql.md`](../icarus-context/docs/implementation-design/platform/02-modelos-de-dados-postgresql.md)
no repositório `icarus-context` (DER — modelo conceitual do MVP, ainda com
pendências de produto marcadas no próprio documento).

**Decisão de idioma:** as entidades/propriedades em C# usam os **mesmos nomes
em português do DER** (`Usuario`, `Epico`, `Campanha`, `Missao`,
`SaldoPontos`...), em vez de traduzir para inglês. Isso é uma exceção
deliberada à convenção usual de nomear identificadores em inglês, para manter
correspondência 1:1 com a linguagem ubíqua do negócio e com os documentos do
`icarus-context`. Decisão tomada em 2026-08-29.

**Convenção física:** os nomes de tabela/coluna no PostgreSQL são gerados
automaticamente em `snake_case` a partir do C# (PascalCase) pelo pacote
[`EFCore.NamingConventions`](https://github.com/efcore/EFCore.NamingConventions)
(`options.UseSnakeCaseNamingConvention()` em `DependencyInjection.cs`). Isso
faz o nome de cada tabela/coluna bater exatamente com o que o DER já usa
(`usuario`, `saldo_pontos`, `usuario_id`...), sem precisar declarar `ToTable`/
`HasColumnName` manualmente em cada propriedade. Os nomes de tabela foram
fixados no singular via `ToTable(...)` em cada `IEntityTypeConfiguration<T>`
para casar exatamente com o DER (que usa `USUARIO`, não `USUARIOS`).

**Campos de auditoria em português:** `CreatedAt`/`UpdatedAt` foram
renomeados para `CriadoEm`/`AtualizadoEm` (colunas `criado_em`/`atualizado_em`)
em 2026-08-29 — regra fixada pelo usuário: **todo campo de tabela é nomeado em
português**, sem exceção (nem os campos técnicos de auditoria).

**Tipos escolhidos** (seguindo a seção 2–3 do DER):
- `Usuario.Id`: `Guid` (`uuid`) — reduz previsibilidade de IDs expostos pela API; não substitui autorização.
- Demais chaves primárias: `long` (`bigint`, `GENERATED BY DEFAULT AS IDENTITY`).
- Datas "de calendário" (`DataNascimento`, `PrazoInicio/Fim`, `VigenciaInicio/Fim`, `DataRegistro`, `PeriodoInicio/Fim`, `DataInicio/Fim` da recorrência, `DataLimite` da missão/missão IA): `DateOnly`/`DateOnly?` (`date`) — tipo idiomático do .NET moderno para data sem hora.
- Instantes (`CriadoEm`, `AtualizadoEm`, `Horario`, `ConcluidaEm`): `DateTimeOffset` (`timestamptz`), conforme exigido pelo DER.
- `Recorrencia.Regra` e `Relatorio.Conteudo`: `string` mapeado para `jsonb` via `HasColumnType("jsonb")`.
- `Missao.Status`, `Ocorrencia.Status`, `MissaoIA.Status`/`StatusAprovacao`, `Epico.Estado`, `Campanha.Estado`, `Item.ControlaApp`, `Relatorio.Tipo`, `MovimentacaoPontos.Tipo`: enums C# persistidos como texto (`HasConversion<string>()`), não como inteiro — mais legível direto no banco e mais fácil de estender.
- `Campanha.ValorMensurado`/`ValorAtual`: `decimal?` (`numeric(12,2)`) — nulo quando a campanha não é mensurável.

**Entidades criadas** (namespace `Icarus.Domain.Entities`): `Usuario`,
`Rotina`, `Epico`, `Campanha`, `Missao`, `MissaoIA`, `Recorrencia`,
`Ocorrencia`, `Diario`, `Relatorio`, `Item`, `InventarioItem`,
`MovimentacaoPontos` — as 13 do DER v1.2 — mais `TokenRenovacao`, criada em
2026-09-06 para autenticação (ver seção 4.4). Total: 14 entidades.

#### DER v1.2 (2026-09-08): ocorrências persistidas de missão

Decisão validada com o usuário: `Recorrencia` é exclusivamente a configuração
de geração; `Ocorrencia` é a execução planejada e persistida de uma missão. A
ativação de uma recorrência gera os próximos 30 dias e uma rotina diária mantém
essa janela. Missões não recorrentes também recebem uma única ocorrência.

`Ocorrencia` possui `MissaoId`, `DataPrevista`, `Status`, `RealizadaEm` e
`ConcluidaEm`, além da auditoria comum. A constraint
`UNIQUE(missao_id, data_prevista)` impede duplicidade na geração. A conclusão
da ocorrência e a respectiva `MovimentacaoPontos` devem ocorrer na mesma
transação; por isso a movimentação referencia `OcorrenciaId`, e não mais
`MissaoId`.

Alterar uma recorrência afeta apenas ocorrências futuras. Inativá-la
exclui/cancela as futuras ou pendentes não vencidas e preserva as atrasadas e
concluídas.

**Correção feita durante o merge (2026-09-14):** a implementação original
desta feature editou a migration `CriarModeloInicial` diretamente em vez de
criar uma nova. Isso funciona só enquanto nenhum banco existe a partir dela —
mas essa migration **já tinha sido commitada e publicada** antes (`826c46d`),
e o banco local deste ambiente já a tinha aplicado. Editar uma migration já
compartilhada é um problema real: quem já rodou `dotnet ef database update`
contra a versão original fica com um `__EFMigrationsHistory` que diz "já
apliquei `CriarModeloInicial`", mas o conteúdo real da migration mudou por
baixo — `dotnet ef database update` não pega as tabelas/colunas novas
sozinho. Ao trazer essa mudança para o branch principal, restauramos
`CriarModeloInicial` (`.cs`, `.Designer.cs`) para o conteúdo original
publicado e criamos uma migration nova (`PersistirOcorrencias`) só com o
delta do `Ocorrencia` — migrations já publicadas não devem ser editadas,
sempre uma nova migration em cima.

**Também corrigido durante o merge:** o DER v1.2 muda o significado de
`Missao.Status` — deixa de ser status de execução (que passa a viver em
`Ocorrencia`) e passa a representar o ciclo da missão (`RASCUNHO`/`ATIVA`/
`INATIVA`, ver RF-04 em `04-requisitos.md`). A implementação original manteve
`Missao.Status` com o enum antigo (`Pendente`/`Concluida`/`Cancelada`/
`ConcluidaComAtraso`), desatualizado em relação ao DER v1.2. Corrigido:
`StatusMissao` agora tem `Rascunho`/`Ativa`/`Inativa`.

#### DER v1.1 (2026-09-05): novos campos e entidade `MissaoIA`

Mudanças identificadas no commit `7c51d46` do `icarus-context` e já
implementadas:

- `Epico`: `+ AreaDaVida` (`string`, obrigatório) e `+ Estado`
  (`EstadoEpico`: `EmAndamento`/`Concluido`/`Excluido`, default `EmAndamento`).
- `Campanha`: `+ ValorMensurado`/`+ ValorAtual` (`decimal?`, `numeric(12,2)`),
  `+ Unidade` (`string?`) e `+ Estado` (`EstadoCampanha`:
  `Planejada`/`EmAndamento`/`Concluida`/`Excluida`, default `Planejada`).
- `Missao`: `+ DataLimite` (`DateOnly?`); enum de status ganhou
  `ConcluidaComAtraso`.
- `Item`: `+ ControlaApp` (`ControlaApp`: `Nao`/`Sim`, default `Nao`) — o DER
  descreve como texto `SIM`/`NAO` (não booleano), então foi modelado como enum
  de texto para seguir o vocabulário do documento, no mesmo padrão dos outros
  campos de status.
- Nova entidade `MissaoIA`: auditoria de missões sugeridas pela IA. Mesmos
  campos de `Missao` (`Titulo`, `Descricao`, `Pontuacao`, `Beneficio`,
  `DataLimite`, `Horario`, `Status`, `ConcluidaEm`) mais `CampanhaId` (nulo =
  sugestão avulsa, mesma regra de `Missao`), `MissaoId` (nulo até a sugestão
  ser aprovada e a missão real ser criada — FK com `ON DELETE SET NULL`) e
  `StatusAprovacao` (`StatusAprovacaoMissaoIA`: `Aprovado`/`Recusado`).

**Interpretações da equipe não explicitadas no DER** (nulidade de campos que o
diagrama Mermaid não marca explicitamente): `AreaDaVida` foi tratado como
obrigatório; `ValorMensurado`/`Unidade`/`ValorAtual` como opcionais (nem toda
campanha é mensurável, ex.: "Dominar Spring Boot" no exemplo do próprio DER);
`DataLimite` como opcional; `ControlaApp` como obrigatório com default `Nao`.
Vale confirmar com quem mantém o `icarus-context` se alguma dessas deveria ser
diferente.

**Outros pontos observados na revisão, sem mudança de código por ora:**
- `InventarioItem.UsuarioId` é redundante com `Item.UsuarioId` (todo item já
  pertence a um usuário) e nada no banco garante que os dois batem — brecha
  de integridade menor, não crítica para o MVP.
- `Recorrencia.Tipo` (coluna) e o campo `"tipo"` usado no exemplo de JSON de
  `Regra` se sobrepõem no próprio DER — ambíguo, mas sem efeito prático hoje.

**Regras de integridade implementadas** (Fluent API, seguindo a seção 26–27 do DER):
- `usuario.email` único;
- `recorrencia.missao_id` único (no máximo uma recorrência por missão);
- `ocorrencia(missao_id, data_prevista)` único (idempotência da geração — seção 27.4 do DER);
- `inventario_item(usuario_id, item_id)` único (uma linha por item no inventário);
- `CHECK` de não-negatividade em `usuario.saldo_pontos`, `missao.pontuacao`, `missao_ia.pontuacao`, `item.valor`, `inventario_item.quantidade`;
- índices de consulta em `rotina(usuario_id, vigencia_inicio, vigencia_fim)`, `missao(usuario_id/campanha_id/horario/status)`, `missao_ia(usuario_id/status_aprovacao)`, `ocorrencia(status)`, `movimentacao_pontos(usuario_id, criado_em)`/`(ocorrencia_id)`, entre outros listados no DER.

**Exclusão em cascata:** todas as relações a partir de `Usuario` (e as
compostas `Epico → Campanha`, `Campanha → Missao`, `Campanha → MissaoIA`,
`Missao → Recorrencia`, `Missao → Ocorrencia`, `Item → InventarioItem`) usam `ON DELETE CASCADE`,
coerente com a decisão do MVP de exclusão física simples (seção 29 do DER).
**Exceções deliberadas:**
- as FKs opcionais de `MovimentacaoPontos` para `Item` e `Ocorrencia` usam
  `ON DELETE RESTRICT` — excluir um item ou uma ocorrência não pode apagar o
  histórico de pontos (extrato imutável, seções 17–18 do DER);
- a FK opcional de `MissaoIA` para `Missao` usa `ON DELETE SET NULL` —
  excluir a missão gerada não precisa apagar o registro de auditoria da
  sugestão que a originou.

Nenhuma dessas estava explícita no DER; são decisões de implementação a
revisitar se o produto definir uma regra diferente.

**Decisões de implementação não fechadas no DER** (assumidas para poder
implementar; revisar quando o produto decidir):
- `Diario`: índice em `(usuario_id, data_registro)`, **sem** `UNIQUE` — o DER deixa em aberto se pode haver mais de uma entrada por dia.
- `Rotina`: nenhuma constraint de não sobreposição de vigência foi criada ainda (o DER sugere isso como possível evolução).

**Migration:** `Persistence/Migrations/..._CriarModeloInicial.cs`, gerada com
`dotnet ef migrations add` e aplicada ao container Postgres local com
`dotnet ef database update`. Comandos usados (rodar a partir de `src/Icarus.Api`):

```bash
dotnet ef migrations add NomeDaMigration --project ../Icarus.Infrastructure --startup-project .
dotnet ef database update --project ../Icarus.Infrastructure --startup-project .
```

`CriarModeloInicial` permanece exatamente como foi publicada originalmente
(seção 8, 2026-09-06) — nunca mais editada depois de commitada/compartilhada.
As mudanças do `Ocorrencia` (DER v1.2, 2026-09-08) e da autenticação (seção
4.4) entram como migrations incrementais próprias em cima dela
(`AdicionarAutenticacao`, `PersistirOcorrencias`), empilhadas normalmente.
`Migrations/*.cs` continua marcado como `generated_code = true` no
`.editorconfig` para os analisadores do .NET não gerarem ruído em código que
nunca é editado à mão.

**`tests/`** — ver seção 4.5 para a convenção completa (um projeto de teste
por camada de produção). Framework: xUnit em todos (versões centralizadas em
`Directory.Packages.props`): `xunit`, `xunit.runner.visualstudio`,
`Microsoft.NET.Test.Sdk`, `coverlet.collector`.

## 4.4 Autenticação (em construção)

Fonte funcional: RF-01 (cadastro) e RF-02 (login/autorização) em
`docs/product-specification/04-requisitos.md`, e seção 10.2 (controles da API)
em `01-arquitetura-da-solucao.md`, ambos no `icarus-context`.

**Lacuna encontrada no DER:** a tabela `USUARIO` do DER não tinha nenhum
campo de senha, apesar do RF-01/RF-02 exigirem hash Argon2id. Adicionamos
`Usuario.SenhaHash` (`string`, `varchar(255)`, guarda o hash Argon2id — nunca
a senha em texto puro, RNF-12) como campo fora do DER original, mesmo padrão
da lacuna do `Ocorrencia` (seção 4.3).

**Divergências deliberadas do MVP documentado** (decisão tomada com o usuário
em 2026-09-06, não são lacunas — o RF-02 original está correto e validado,
optamos por ir além dele):

- **Access token curto + refresh token de longa duração**, em vez do "JWT de
  24h sem renovação" do RF-02. Para isso criamos a entidade `TokenRenovacao`
  (`Id`, `UsuarioId`, `TokenHash`, `ExpiraEm`, `RevogadoEm`,
  `SubstituidoPorId`) — guarda só o **hash** do refresh token (nunca o valor
  em texto puro, mesmo cuidado da senha), com **rotação**: cada uso de um
  refresh token gera um par novo e marca o antigo como substituído
  (`SubstituidoPorId`), permitindo detectar reuso indevido de um token já
  trocado. FK para `Usuario` em cascade (apagar o usuário apaga seus tokens);
  FK própria (`SubstituidoPor`) em `Restrict` — o token antigo nunca é
  apagado, só marcado, para preservar o histórico/auditoria da rotação.
  `TokenHash` é único (`ix_token_renovacao_token_hash`).
- **Validação de senha mantida como o RF-01.2 já define** (8 a 128
  caracteres, **sem** regra obrigatória de composição maiúscula/minúscula/
  número/especial) — o usuário considerou adicionar regra de composição, mas
  decidimos manter o que já está validado: exigir composição é uma prática
  ultrapassada (NIST SP 800-63B e OWASP recomendam hoje comprimento sobre
  composição, já que regras de composição levam a senhas previsíveis).
- **Sem adoção do pacote `Microsoft.AspNetCore.Identity`** — o "padrão de
  prateleira" do .NET para cadastro/login, mas seu esquema de tabelas
  (`AspNetUsers` etc., nomes em inglês) bateria de frente com a regra da
  seção 0 (campos de tabela sempre em português, entidades espelhando o
  DER). Construímos autenticação por cima de peças padrão do .NET
  (`Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.RateLimiting`
  para o limite de tentativas do RF-02) sem adotar o *user store* do
  `Identity` inteiro.

**Migration:** `AdicionarAutenticacao` (2026-09-06), aplicada de forma
incremental sobre `CriarModeloInicial` (não foi mais substituída — a partir
daqui as migrations passam a ser empilhadas normalmente, já que
`CriarModeloInicial` já foi publicada/compartilhada). Validado: 13 tabelas
(`usuario` com `senha_hash`, mais `token_renovacao`) conferidas via `psql`;
`dotnet build` (0 avisos/erros) e `dotnet test` passando.

**Hash de senha (Argon2id):** implementado via `IPasswordHasher`
(`Icarus.Application/Interfaces/IPasswordHasher.cs`), com implementação
`PasswordHasher` em `Icarus.Infrastructure/Security/` usando o pacote
`Konscious.Security.Cryptography.Argon2` (registrado como `AddSingleton`,
já que não guarda estado). Parâmetros de custo seguem a segunda recomendação
do RFC 9106 (m=65536 KiB, t=3, p=4). O hash é serializado como uma única
string autodescritiva (parâmetros + salt + hash em Base64, formato inspirado
no PHC string format), permitindo aumentar o custo no futuro sem invalidar
hashes já salvos. Comparação em `Verify` usa
`CryptographicOperations.FixedTimeEquals` (tempo constante, evita vazar por
timing em qual byte o hash diverge). Testado em
`tests/Icarus.Infrastructure.UnitTests/Security/PasswordHasherTests.cs`
(hashes diferentes para a mesma senha graças ao salt aleatório; verificação
correta/incorreta).

Ajuste de tooling: `.editorconfig` ganhou uma regra `[tests/**/*.cs]` com
`dotnet_diagnostic.CA1707.severity = none` — os analisadores por padrão
reclamam de underscore em nome de método (`CA1707`), mas a convenção
`Metodo_Cenario_ResultadoEsperado` é comum e mais legível especificamente
para testes; desligamos a regra só nos projetos de teste, mantendo o padrão
em código de produção.

**Caso de uso de Cadastro (RF-01):** primeiro código real em
`Icarus.Application`. Estrutura criada:
- `Interfaces/IUsuarioRepository.cs` e `Interfaces/IUnitOfWork.cs`:
  contratos de persistência — `Icarus.Application` não pode referenciar
  `Icarus.Infrastructure` (inverteria a Clean Architecture), então define o
  que precisa e deixa a implementação concreta (EF Core) para a
  Infrastructure. `IUnitOfWork` fica separado do repositório porque fluxos
  futuros (ex.: concluir uma ocorrência + lançar a movimentação de pontos)
  precisam gravar mais de uma entidade na mesma transação.
- `UseCases/RegisterUser/`: `RegisterUserCommand` (entrada), `RegisterUserResult`
  (saída — só o `UsuarioId`) e `RegisterUserUseCase` (normaliza o e-mail,
  checa duplicidade, gera o hash da senha via `IPasswordHasher`, persiste;
  assume entrada já validada pelo chamador — a validação fica na borda HTTP,
  ver "Endpoint de cadastro" abaixo). Nomes de tipos em inglês (regra da
  seção 0 — não é modelo de dados), propriedades em português (mesmo padrão
  de `IPasswordHasher`).
- `Exceptions/EmailJaCadastradoException.cs`: lançada na duplicidade de
  e-mail (RF-01.1); traduzida para HTTP 409 pelo `GlobalExceptionHandler` da
  API.
- `Icarus.Infrastructure/Persistence/Repositories/UsuarioRepository.cs`:
  implementação com EF Core. `IcarusDbContext` passou a implementar
  `IUnitOfWork` diretamente (implementação explícita de interface, não polui
  a API pública do `DbContext`).
- `Icarus.Application/DependencyInjection.cs` (`AddApplication`, primeiro DI
  dessa camada) registra o caso de uso; chamado em `Program.cs` junto com
  `AddApi` e `AddInfrastructure` (um `DependencyInjection` por camada, padrão
  consolidado da comunidade .NET).

Testado em `RegisterUserUseCaseTests.cs` com um repositório falso em memória
(`UsuarioRepositoryEmMemoria`), sem precisar de Postgres real: e-mail
normalizado (trim + minúsculas), senha nunca fica igual ao hash salvo, e
e-mail duplicado lança `EmailJaCadastradoException`.

#### Dúvida em aberto: `IUnitOfWork` deveria ser uma classe própria?

Hoje `IcarusDbContext` implementa `IUnitOfWork` diretamente (implementação
explícita de interface — `SalvarAlteracoesAsync` só repassa para
`SaveChangesAsync`, sem nenhum comportamento extra). Isso **não é** a única
opção nem existe um "padrão único da comunidade .NET" aqui — são pelo menos
três escolas diferentes, todas com bom respaldo:

1. **Nem repositório nem unit of work** — posição de referências influentes
   como Jimmy Bogard (autor do MediatR/AutoMapper): `DbSet<T>` já é um
   repositório, `DbContext` já é um unit of work; casos de uso deveriam usar
   o `DbContext` direto, sem camadas extras.
2. **Uma interface sobre o `DbContext`, sem repositório por entidade** — é o
   que o template de Clean Architecture do Jason Taylor faz hoje (um dos mais
   usados do ecossistema .NET): `IApplicationDbContext` implementada pelo
   `DbContext`, injetada direto nos casos de uso.
3. **Repositório + Unit of Work explícitos** — orientação do e-book oficial
   "Architecting Modern Web Applications with ASP.NET Core" da Microsoft e do
   eShopOnContainers (repositório de referência da própria Microsoft). É o
   caminho que escolhemos aqui. Dentro dessa escola, o próprio
   eShopOnContainers implementa `IUnitOfWork` direto no `DbContext` — mesma
   escolha que fizemos —, mas outros exemplos da mesma escola usam uma classe
   `UnitOfWork` separada.

**Por que ficamos com `DbContext` implementando direto (opção "dentro"), por
enquanto:** `SalvarAlteracoesAsync` hoje não faz nada além de chamar
`SaveChangesAsync` — criar uma classe `UnitOfWork` só para repassar essa
chamada seria indireção sem ganho (YAGNI). A extração para uma classe própria
é mecânica e rápida (mover o método, trocar o registro no DI) quando/se
`SalvarAlteracoesAsync` precisar fazer mais do que salvar — os candidatos
mais concretos e já previstos no `icarus-context`:
- disparar eventos de domínio depois de um save bem-sucedido (ex.: "ocorrência
  concluída" notificando outra parte do código);
- padrão outbox (seção 8.3 da arquitetura): gravar o estado **e** a mensagem
  de saída para o RabbitMQ na mesma transação.

**Decisão a retomar:** quando o primeiro desses casos concretos aparecer
(provavelmente no fluxo de concluir uma ocorrência), decidir ali se vale a
pena extrair uma classe `UnitOfWork` própria naquele momento.

#### Decisão consciente de simplificação: sem MediatR/CQRS, exceção em vez de Result pattern

Duas escolhas de estrutura do caso de uso que divergem do padrão mais
replicado hoje na comunidade .NET, feitas deliberadamente para reduzir a
quantidade de conceitos novos enquanto o usuário aprende, registradas aqui
para reavaliar mais adiante:

- **Sem MediatR/CQRS.** O template de Clean Architecture do Jason Taylor —
  hoje um dos projetos .NET mais replicados do GitHub — usa `IMediator` para
  despachar `Commands`/`Queries` a uma classe `Handler` própria por caso de
  uso, desacoplando o Controller de qual classe trata cada operação. Aqui o
  Controller vai chamar `RegisterUserUseCase` diretamente (chamada explícita,
  mais fácil de seguir com "Ir para definição"). Se o número de casos de uso
  crescer muito, ou se for necessário compor comportamento transversal
  (logging, validação, transação) de forma uniforme em todos eles sem
  repetir código em cada Controller, vale reconsiderar o MediatR — ele
  resolve isso com *pipeline behaviors*.
- **Exceção em vez de Result pattern.** Usamos `EmailJaCadastradoException`
  para uma falha de negócio esperada (e-mail duplicado não é bug). Existe
  uma tendência crescente na comunidade de usar um tipo `Result<T>`/
  `ErrorOr<T>` para esses casos, reservando exceção só para o
  verdadeiramente excepcional — pacotes como `ErrorOr` e `FluentResults` são
  comumente citados. Vale reconsiderar se o número de regras de negócio
  "esperadas para falhar" crescer a ponto de o tratamento por exceção global
  ficar difícil de manter previsível.

#### Endpoint de cadastro: `POST /api/auth/cadastro`

Primeiro Controller da solução (`Icarus.Api/Controllers/AuthController.cs`,
ação `Register`). Controller fino: traduz HTTP para o `RegisterUserUseCase` e
de volta, sem regra de negócio. Resposta de sucesso: `201 Created` com corpo
`{ "usuarioId": "..." }` e **sem** header `Location` (ver pendências abaixo).
Todas as decisões abaixo foram validadas uma a uma com o usuário em
2026-09-27:

- **Rota:** `/api/auth/cadastro` — exceção deliberada à regra "inglês fora do
  modelo de dados": `auth` em inglês e a ação em português, por ser o termo
  literal do RF-01 (sugestão original era `register`). As demais rotas
  seguirão termos técnicos consagrados: `/api/auth/login`, `/refresh`,
  `/logout`. Nomes de classes/métodos (`AuthController.Register`) continuam em
  inglês; só a string da rota é português.
- **DTOs HTTP próprios** em `Icarus.Api/Contracts/Auth/` (`RegisterRequest`,
  `RegisterResponse`), desacoplados do `RegisterUserCommand`: o contrato
  público com o app mobile pode mudar sem quebrar o Application e vice-versa,
  e nenhum campo interno vaza por acidente. Custo: um mapeamento Request para
  Command no Controller. JSON em camelCase (padrão do ASP.NET Core).
- **Validação na borda, com filtro reutilizável:** `Filters/ValidationFilter`
  (global, `IAsyncActionFilter`) procura um `IValidator<T>` para cada
  argumento da action e, se falhar, responde `400` com
  `ValidationProblemDetails` sem executar a action — chaves dos erros em
  camelCase, iguais aos campos do JSON. Como o filtro enxerga os argumentos da
  action (o `RegisterRequest`), o validador foi **movido do Application para a
  API** (`RegisterRequestValidator`, ao lado do Request); o
  `RegisterUserValidator` antigo foi removido e o pacote `FluentValidation`
  passou do `Icarus.Application` para o `Icarus.Api`. Consequência aceita: o
  `RegisterUserUseCase` confia que o chamador validou — qualquer futuro
  chamador que não seja a API HTTP (ex.: um consumidor de mensagens) precisa
  validar por conta própria. Novos validadores exigem uma linha de registro
  em `AddApi()`; quando houver vários, vale trocar por varredura de assembly
  (pacote `FluentValidation.DependencyInjectionExtensions`).
- **Mensagens de validação em português:** cultura `pt-BR` global do
  FluentValidation (traduções já embutidas), com `WithName(...)` para nomes
  amigáveis ("E-mail", "Data de nascimento"). Para o "campo ausente" cair no
  FluentValidation (e não no `required` implícito do MVC, que responderia
  antes, em inglês), `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`
  está ligado. Erros de *formato* do JSON (JSON malformado, data que não é
  data) continuam sendo respondidos pelo próprio ASP.NET Core, em inglês
  técnico, mas ainda como `ProblemDetails` 400.
- **Erros HTTP:** `ExceptionHandling/GlobalExceptionHandler`
  (`IExceptionHandler` + `AddProblemDetails()`, padrão do .NET 8+) mapeia
  `EmailJaCadastradoException` para `409 ProblemDetails`; qualquer outra
  exceção devolve `false` e cai no `500` genérico padrão, sem detalhe interno
  (seção 10.1 da arquitetura). `UseStatusCodePages()` deixa 404/405 etc. também
  em `ProblemDetails`. Controllers ficam sem `try/catch`.
- **Terceiro `DependencyInjection`:** `Icarus.Api/DependencyInjection.cs`
  (`AddApi()`) concentra controllers (com o filtro), OpenAPI, `ProblemDetails`,
  exception handler e validadores; o `Program.cs` só encadeia
  `AddApi`/`AddApplication`/`AddInfrastructure` e monta o pipeline.
- **Sem versionamento de API por ora** (um único cliente, MVP local); rotas
  seguem `/api/...`. Adicionar `Asp.Versioning` depois é viável.
- **Testes:** `RegisterRequestValidatorTests` (regras do RF-01.2, inclusive
  "sem regra de composição"). Testes de integração do endpoint ficam para um
  passo dedicado com `WebApplicationFactory` + Testcontainers (PostgreSQL
  real, exigido pelo RNF-04) — por ora, verificação manual.

**Trade-off de segurança aceito (enumeração de contas):** o `409` revela que
um e-mail já está cadastrado. É o que o RF-01.1 pede ("e-mail duplicado é
rejeitado"), mas a OWASP recomenda respostas genéricas nesse cenário; uma
resposta genérica exigiria verificação de e-mail, fora do MVP. O limite de
tentativas do RF-02 vale para o login, não para o cadastro — considerar rate
limiting no cadastro no passo 8.

**Pendência registrada (a fazer assim que possível, logo após login/JWT):**
`GET /api/usuarios/{id}` (perfil), com `[Authorize]` e checagem de que o `id`
é o do dono do token (RF-02.4: identificador de outro usuário não pode
consultar nem alterar o recurso; responder 404 para não confirmar a
existência), e então adicionar o header `Location` (URL do recurso recém-criado,
convenção REST para `201 Created`) na resposta do cadastro. Não foi feito antes
porque um GET de perfil sem autenticação exporia nome, e-mail e data de
nascimento de qualquer usuário por id.

**Ferramenta para testar a API manualmente: Swagger UI (único jeito, desde
2026-10-10).** Não há um padrão único da comunidade pra isso. Passamos por
três fases:

1. **`.http`** (template de primeira parte, roda no Visual Studio/VS Code/
   Rider sem pacote extra) — reescrito com os casos de cadastro, login,
   `me` e refresh conforme cada um foi implementado.
2. **Scalar** (`Scalar.AspNetCore`) — ligado em 2026-10-10 porque o usuário
   queria algo visual, não só o `.http`.
3. **Swagger UI** (Swashbuckle) — trocamos o Scalar por ele no mesmo dia, a
   pedido do usuário (ver abaixo). Depois disso, o usuário pediu pra
   **remover o `.http` definitivamente** ("também não vamos mais usar o
   `.http`") — arquivo `src/Icarus.Api/Icarus.Api.http` excluído. Hoje o
   Swagger UI é a única ferramenta de teste manual do projeto.

Mapeado só em `Development`. `launchSettings.json` tem `launchBrowser: true`
nos dois perfis (`http`/`https`) — ao rodar `dotnet run` (ou F5 no Visual
Studio), o navegador já abre direto nele.

Primeiro ligamos o **Scalar** (`Scalar.AspNetCore`,
`app.MapScalarApiReference()`, rota `/scalar/v1`, `launchUrl: "scalar/v1"`).
No mesmo dia, a pedido do usuário, **trocamos para o Swagger UI** — pacote e
código do Scalar removidos. Fica documentado aqui como religar o Scalar se
quisermos voltar:
```csharp
// Program.cs, dentro do if (app.Environment.IsDevelopment())
app.MapScalarApiReference(); // precisa do pacote Scalar.AspNetCore
```
e trocar `launchUrl` de `"swagger"` para `"scalar/v1"` no `launchSettings.json`.

**Swagger UI (Swashbuckle), ligado hoje:** só o pacote
`Swashbuckle.AspNetCore.SwaggerUI` — **sem** `AddSwaggerGen`/`UseSwagger`, já
que não precisamos de um segundo gerador de documento OpenAPI; o
`Microsoft.AspNetCore.OpenApi` (`AddOpenApi()`/`MapOpenApi()`) que já
existia continua gerando o documento (`/openapi/v1.json`), e o Swagger UI só
lê esse documento (`options.SwaggerEndpoint("/openapi/v1.json", "Icarus API")`).
Rota padrão `/swagger` (redireciona para `/swagger/index.html`);
`launchUrl: "swagger"`. Validado: build limpo, `/swagger/index.html`
responde `200` com a UI de verdade, listando `/api/Auth/cadastro` e
`/api/Auth/login` a partir do mesmo `/openapi/v1.json`.

**Botão "Authorize" (2026-10-10), a pedido do usuário.** Sem `AddSwaggerGen`,
não existe `AddSecurityDefinition`/`AddSecurityRequirement` do Swashbuckle
disponível — essas APIs só existem quando o Swashbuckle também gera o
documento. Como o documento é gerado pelo `Microsoft.AspNetCore.OpenApi`, a
definição do esquema de segurança precisa entrar nele por outro caminho: um
*document/operation transformer*
(`Icarus.Api/OpenApi/BearerSecuritySchemeTransformer.cs`), registrado via
`AddOpenApi(options => { options.AddDocumentTransformer<...>();
options.AddOperationTransformer<...>(); })`:
- O lado de **documento** adiciona `components.securitySchemes.Bearer`
  (tipo `http`, scheme `bearer`, `bearerFormat: JWT`) — só se o esquema
  `JwtBearer` estiver de fato registrado (`IAuthenticationSchemeProvider`).
- O lado de **operação** olha se a action tem `[Authorize]`
  (`IAuthorizeData` nos metadados do endpoint) e só então adiciona o
  requisito de segurança **naquela** operação — por isso só `GET /api/auth/me`
  ganha o cadeado no Swagger UI; cadastro e login continuam sem, do jeito
  que têm que ser.
- `Microsoft.OpenApi` 2.7.5 (dependência do `Microsoft.AspNetCore.OpenApi`
  10.0.11) é uma versão nova, com API bem diferente da v1 clássica do
  Swashbuckle: namespace achatado (sem `Microsoft.OpenApi.Models`),
  `OpenApiSecuritySchemeReference(nomeDoEsquema, document)` em vez de um
  `Reference` separado — tive que compilar e ajustar duas vezes até achar
  os nomes certos, incluindo passar o `context.Document` de verdade pro
  construtor da referência (passar `null` ali serializava a referência como
  objeto vazio `{}`, quebrando o requisito de segurança silenciosamente).
- `options.ConfigObject.PersistAuthorization = true` no `UseSwaggerUI`
  (`SwaggerUIOptions.PersistAuthorization` não existe direto — fica dentro
  de `ConfigObject`) — mantém o token colado no botão entre um F5 e outro
  na página.

Validado: documento OpenAPI com `components.securitySchemes.Bearer`
presente; `security` só aparece em `GET /api/auth/me` (`[{"Bearer": []}]`),
ausente em `cadastro`/`login`; fluxo completo cadastro → login → `/eu` com
o `accessToken` colado no header `Authorization` continua devolvendo o
`UsuarioId` certo. `dotnet build` (0 avisos/erros) e `dotnet test`
(17 aprovados) revalidados.

**Verificação ponta a ponta (2026-09-27, Postgres real, API em `dotnet run`,
`curl`):** `201` com `{ "usuarioId": ... }` (linha gravada com nome/e-mail
normalizados, hash `$argon2id$...` de 100 caracteres, `criado_em`/`atualizado_em`
preenchidos); e-mail repetido em maiúsculas devolve `409` `ProblemDetails`;
dados inválidos e corpo vazio devolvem `400` com `errors` por campo em
camelCase e mensagens em português; JSON malformado e data inválida devolvem
`400` do próprio ASP.NET Core; rota inexistente devolve `404` `ProblemDetails`;
nenhum caso inválido criou linha. Usuário de teste removido depois.

**Detalhes conhecidos, a decidir (não bloqueiam):**
- Os campos `title` do `ProblemDetails` gerados pelo framework continuam em
  inglês ("One or more validation errors occurred.", "Not Found"); só as
  mensagens de validação por campo estão em português. Traduzir exige
  customizar `ProblemDetailsOptions`/`ApiBehaviorOptions`.
- As mensagens de erro de *binding* (JSON malformado, data inválida) são do
  ASP.NET Core e incluem caminho/posição do JSON e, no caso da data, o nome
  completo do tipo interno (`Icarus.Api.Contracts.Auth.RegisterRequest`) —
  pequeno vazamento de detalhe interno frente à seção 10.1 da arquitetura
  ("mensagens sem exposição de detalhes internos"). Solução possível:
  substituir essas mensagens por um texto genérico no
  `InvalidModelStateResponseFactory`.
- A tradução pt-BR embutida do FluentValidation usa "deve ser informado"
  (sem concordância de gênero: "'Senha' deve ser informado").

**Ainda não implementado** (na época desta seção; ver "Passo 4" abaixo para o
que foi feito depois): geração de JWT, casos de uso e endpoints de
login/refresh/logout, `JwtBearer` + rate limiting de login, `GET` de perfil e
`Location` (pendência acima), testes de integração do endpoint.

### Passo 4 do plano de autenticação: login + emissão de token (RF-02.1)

Escopo desta etapa, validado com o usuário antes de implementar: só o login
e a emissão dos tokens. Logout (RF-02.2), limite de tentativas (RF-02.3) e o
middleware de autorização/ownership (RF-02.4) ficam para os próximos passos —
mesmo racional de dividir o cadastro em pedaços revisáveis.

**Geração do JWT:** `IJwtTokenGenerator`
(`Icarus.Application/Interfaces/IJwtTokenGenerator.cs`), implementado por
`JwtTokenGenerator` em `Icarus.Infrastructure/Security/` usando
`Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler` — mais rápida que
o antigo `JwtSecurityTokenHandler` (API mais simples, não usa as classes
XML-orientadas antigas) e o mesmo handler que o
`Microsoft.AspNetCore.Authentication.JwtBearer` já usa por padrão para
*validar* tokens desde o .NET 8; gerar com a mesma classe mantém os dois
lados consistentes. Claim única no payload: `NameIdentifier` com o
`UsuarioId` — nenhum outro dado pessoal, já que o JWT é só assinado, não
criptografado, e qualquer um pode decodificar o payload (não é um cofre).

**Durações** (validadas com o usuário): access token de **15 minutos**,
refresh token de **7 dias**. Configuração de assinatura (`Jwt:ChaveSecreta`,
`Jwt:Emissor`, `Jwt:Audiencia`) via `JwtOptions` (`IOptions<T>`), nunca em
`appsettings.json` — segue a mesma convenção já usada para a connection
string: `dotnet user-secrets` no desenvolvimento.

**Refresh token:** string aleatória de 32 bytes (`RandomNumberGenerator`),
devolvida ao cliente em texto puro uma única vez; só o hash é persistido em
`TokenRenovacao.TokenHash`. O hash usa **SHA-256 simples, não Argon2id** —
diferente da senha (escolhida por um humano, previsível, precisa de uma
função de derivação lenta contra força bruta), o refresh token já nasce
aleatório de alta entropia, então hashear com Argon2id seria custo de CPU
desperdiçado sem ganho de segurança real (mesmo racional usado para hash de
API keys/tokens de sessão na prática da comunidade).

**Caso de uso de Login:** `LoginUseCase`
(`Icarus.Application/UseCases/Login/`) busca o usuário por e-mail
(`IUsuarioRepository.BuscarPorEmailAsync`, método novo), verifica a senha via
`IPasswordHasher.Verify` e, se tudo certo, gera os dois tokens e persiste o
refresh token (`ITokenRenovacaoRepository.AdicionarAsync`, repositório novo).
**E-mail inexistente e senha incorreta lançam a mesma exceção**
(`CredenciaisInvalidasException`, mapeada para `401` com a mensagem genérica
"E-mail ou senha inválidos.") — não diferenciar os dois casos evita revelar
se um e-mail está cadastrado, mesmo princípio por trás do RF-02.3. O log de
falha também é genérico, sem e-mail.

**Endpoint:** `POST /api/auth/login` (`AuthController.Login`), seguindo a
mesma estrutura do cadastro — `LoginRequest`/`LoginResponse` próprios,
`LoginRequestValidator` (FluentValidation, só confirma formato: não valida o
tamanho da senha, regra que só se aplica ao cadastro) registrado via
`ValidationFilter` existente.

Testado em `LoginUseCaseTests.cs` (sucesso gera e persiste os tokens;
e-mail inexistente e senha incorreta lançam a mesma exceção) e em
`JwtTokenGeneratorTests.cs` (o JWT gerado valida de verdade com
`JsonWebTokenHandler.ValidateTokenAsync` e carrega o `UsuarioId` certo; o
hash do refresh token bate com SHA-256 do valor devolvido; dois refresh
tokens gerados em sequência são diferentes).

**Verificação ponta a ponta (2026-10-06, Postgres real, API em `dotnet run`,
`curl`):** cadastro seguido de login devolve `200` com `accessToken` (JWT
válido) e `refreshToken`; `token_renovacao.token_hash` no banco **difere**
do `refreshToken` devolvido na resposta (confirma que só o hash foi
persistido); login com senha errada e login com e-mail inexistente devolvem
o mesmo `401` com a mesma mensagem; os dois casos de falha geram a mesma
linha de log genérica ("Tentativa de login rejeitada: credenciais
inválidas"), sem e-mail. `dotnet build` (0 avisos/erros) e `dotnet test`
(17 aprovados) revalidados. Usuário de teste removido depois.

**Ainda não implementado (nesta etapa):** logout (RF-02.2), limite de
tentativas de login (RF-02.3, `Microsoft.AspNetCore.RateLimiting`), `GET`
de perfil completo e testes de integração. O middleware de autorização
(`AddJwtBearer` + `[Authorize]`) e o endpoint de refresh (com rotação e
detecção de reuso) foram implementados depois, no mesmo dia — ver "RF-02.4"
e "Refresh" abaixo.

### Refresh: rotação de token + detecção de reuso

Implementado em 2026-10-10, a pedido do usuário ("vamos pro refresh token
agora"). Antes de implementar, uma decisão de segurança real foi validada
com ele: o que fazer quando um refresh token **já rotacionado antes**
aparece de novo numa requisição (indício de token roubado, usado em
paralelo ao dono legítimo)? Duas opções honestas:

1. Rejeitar só aquela tentativa (mais simples, não reage ao indício de roubo).
2. **Revogar todos os refresh tokens ativos do usuário** (recomendação do
   OAuth2 Security BCP, prática real do Auth0/Okta) — reage ao pior
   cenário, ao custo de deslogar sessões legítimas também.

Optamos pela opção 2.

**`POST /api/auth/refresh`** (`AuthController.Refresh`, reaproveita
`LoginResponse` — mesmo formato de saída, sem motivo de negócio pra serem
tipos diferentes). `RefreshTokenUseCase`
(`Icarus.Application/UseCases/RefreshToken/`):
- Calcula o hash do token recebido (`IJwtTokenGenerator.CalcularHashDoRefreshToken`,
  novo método — mesma lógica SHA-256 já usada em `GerarRefreshToken`, agora
  exposta separadamente pra poder recalcular a partir de um token existente)
  e busca por igualdade direta no banco (`ITokenRenovacaoRepository.BuscarPorTokenHashAsync`
  — possível porque o hash é determinístico, ao contrário do Argon2id da
  senha, que exige buscar por outra chave e then verificar).
- Token não encontrado, expirado ou já revogado → `RefreshTokenInvalidoException`
  (401 genérico, mesma mensagem pros três casos — não dá pista de qual
  aconteceu, mesmo racional do login).
- Token já tem `SubstituidoPorId` preenchido (já foi trocado antes) →
  **reuso detectado**: chama `ITokenRenovacaoRepository.RevogarTodosDoUsuarioAsync`
  (marca `RevogadoEm` em todo token ainda ativo daquele usuário) e lança a
  mesma exceção genérica.
- Token válido → gera o par novo (`GerarAccessToken` + `GerarRefreshToken`),
  cria a nova `TokenRenovacao`, e liga `tokenAntigo.SubstituidoPor = tokenNovo`
  (navegação do EF Core — resolve o `SubstituidoPorId` automaticamente ao
  salvar, sem precisar de um segundo `SaveChanges` pra obter o Id gerado).
  **Não** marca `RevogadoEm` nessa rotação normal — `SubstituidoPorId` e
  `RevogadoEm` são conceitos diferentes (superado por rotação vs. revogado
  de verdade), só o segundo dispara em logout/roubo.

Testado em `RefreshTokenUseCaseTests` (rotação válida; token inexistente,
expirado e revogado lançam a exceção; reuso lança a exceção **e** revoga
outro token ativo do mesmo usuário) e `JwtTokenGeneratorTests`
(`CalcularHashDoRefreshToken` bate com o hash que `GerarRefreshToken` já
tinha gerado).

**Verificação ponta a ponta (2026-10-10, Postgres real, API em `dotnet run`,
`curl`):** cadastro → login → refresh devolveu um par **novo** de tokens
(`200`); reapresentar o token original (já rotacionado) devolveu `401`;
o token novo que tinha sido emitido no passo de refresh **também** passou
a devolver `401` depois disso — confirmando que a reação ao reuso revogou
mesmo a sessão inteira, não só o token reusado. Consulta direta no banco
mostrou os 4 tokens do usuário (de dois ciclos de login/refresh diferentes)
com `revogado_em` preenchido no mesmo instante. `dotnet build`
(0 avisos/erros) e `dotnet test` (23 aprovados) revalidados.

**Ainda não implementado (na época):** a notificação para o usuário de que
a sessão foi revogada por suspeita de roubo (hoje ele só recebe `401`
igual a qualquer token inválido — não há um alerta distinto); endpoint de
logout, implementado depois, no mesmo dia — ver "Logout" abaixo.

### Logout (RF-02.2)

Implementado em 2026-10-10, a pedido do usuário. Duas decisões validadas
com ele antes de implementar:

1. **Exige só o refresh token, não o access token** (sem `[Authorize]`) —
   mesmo racional do refresh: o refresh token em si já é a credencial. Exigir
   também um access token válido forçaria um refresh só para poder deslogar,
   se o access token (15 min) já tivesse expirado no momento do logout.
2. **Resposta sempre `204`, independente do estado do token** — segue a
   RFC 7009 (revogação de token OAuth2): o servidor responde sucesso mesmo
   se o token já não existia, já expirou ou já tinha sido revogado antes,
   para não revelar o estado de um token a quem pede o logout. O resultado
   desejado ("esse token não funciona mais") já é verdade nos três casos.

**`POST /api/auth/logout`** (`AuthController.Logout`, reusa `RefreshRequest`
— mesmo formato de entrada do refresh). `LogoutUseCase`
(`Icarus.Application/UseCases/Logout/`) calcula o hash do token recebido,
busca por `ITokenRenovacaoRepository.BuscarPorTokenHashAsync` (método já
existente, criado pro refresh) e, se encontrar um token ainda não revogado,
marca `RevogadoEm`. Não lança nenhuma exceção em caso algum — não há
"caminho de erro" nesse caso de uso, por design. Diferente do `ExecuteAsync`
dos outros casos de uso, não devolve nada (`Task`, não `Task<T>`): não há
dado útil para devolver depois de um logout.

Testado em `LogoutUseCaseTests` (token válido marca `RevogadoEm` e salva;
token inexistente e token já revogado não lançam exceção e não salvam de
novo). **Verificação ponta a ponta** (Postgres real, `curl`): cadastro →
login → logout devolveu `204`; usar o mesmo refresh token depois num
refresh devolveu `401` (confirma que a revogação funcionou de verdade);
logout de novo com o mesmo token (já revogado) devolveu `204` outra vez
(idempotente); logout com um token que nunca existiu também devolveu
`204`, sem diferença na resposta. `dotnet build` (0 avisos/erros) e
`dotnet test` (26 aprovados) revalidados.

### Limite de tentativas de login (RF-02.3)

Implementado em 2026-10-11, a pedido do usuário ("vamos para o limite de
tentativas"). Antes de implementar, o usuário pediu pra eu esclarecer qual
é de fato o padrão da comunidade .NET pra esse problema — a resposta tinha
uma nuance importante:

**`Microsoft.AspNetCore.RateLimiting`** (mencionado como ferramenta
planejada em versões anteriores desta seção) resolve um problema
*diferente*: limitar a taxa de requisições por IP/rota, de forma genérica.
Ele roda antes do corpo da requisição ser lido, então não vê o e-mail
dentro do JSON, e por padrão contaria *toda* requisição, não só as que
falharam — não modela "5 falhas por **conta** + origem" sem engenharia
extra que descaracterizaria a ferramenta.

**O padrão real da comunidade .NET pra esse problema específico** é o
*lockout* do `ASP.NET Core Identity` (`UserManager.AccessFailedCount` +
`LockoutEnd`, por conta, persistido na tabela do usuário) — só que sem o
componente de IP, que o Identity não tem por padrão. Como não adotamos o
Identity (seção 4.4, decisão já documentada — conflito de nomenclatura de
tabelas), construímos o equivalente conceitual na mão, com o IP adicionado,
igual já fizemos pra hash de senha e JWT.

**Implementação:** `ILoginAttemptLimiter`
(`Icarus.Application/Interfaces/`), com três operações:
`EstaBloqueado`, `RegistrarFalha`, `RegistrarSucesso` (chave = e-mail
normalizado + origem). Implementado em `Icarus.Infrastructure/Security/LoginAttemptLimiter.cs`
usando `IMemoryCache` — **aceito como limitação conhecida do MVP local**:
o contador se perde se a API reiniciar. Essa não é uma decisão nova: já
estava documentada no `icarus-context`
(`01-arquitetura-da-solucao.md`, seção 17 — "Limite de login em memória se
perder ao reiniciar: Baixo no MVP local; aceitar como limitação
documentada; persistir ou externalizar se houver publicação"). Janela
fixa de 15 min a partir da primeira falha (expira sozinha no cache); 5
falhas bloqueiam; um login certo zera o contador (uma conta não devia
continuar penalizada por falhas antigas depois de autenticar com sucesso).

`LoginCommand` ganhou o campo `Origem` (o IP do cliente,
`HttpContext.Connection.RemoteIpAddress`, extraído no `AuthController` —
`Icarus.Application` não pode depender de `HttpContext`).
`LoginUseCase.ExecuteAsync` checa `EstaBloqueado` **antes** de buscar o
usuário ou calcular qualquer hash Argon2id — evita gastar CPU numa
requisição que já vai ser rejeitada. Nova exceção
`LimiteDeTentativasExcedidoException`, mapeada para `429 Too Many
Requests` (RFC 6585) — não `401`, porque `429` é o código HTTP padrão pra
essa situação e, diferente de inventar uma resposta "disfarçada" de
credenciais inválidas, não revela nada sobre a conta além de "está sendo
limitado agora".

Testado em `LoginUseCaseTests` (bloqueia na 6ª tentativa mesmo com a senha
certa; um sucesso zera o contador) e `LoginAttemptLimiterTests`
(implementação real com `IMemoryCache`: não bloqueia com menos de 5
falhas, bloqueia a partir de 5, `RegistrarSucesso` zera, contadores de
e-mails/origens diferentes não se misturam). **Verificação ponta a ponta**
(Postgres real, `curl`): 5 tentativas com senha errada devolvem `401`; a
6ª tentativa, com a senha **certa**, devolve `429` (prova que bloqueou
antes de checar a senha); outro usuário, do mesmo IP, loga normalmente
(prova que o bloqueio é por conta+origem, não por IP sozinho). `dotnet
build` (0 avisos/erros) e `dotnet test` (33 aprovados) validados.

**Ainda não implementado:** liberar o bloqueio manualmente (hoje só expira
pelo tempo); métrica/alerta de observabilidade quando uma conta é
bloqueada (hoje só tem o log `Warning`).

### `GET` de perfil (RF-01) + `Location` no cadastro (2026-10-11)

Implementado a pedido do usuário — a última peça deixada pendente desde o
cadastro, porque expor dados pessoais sem autenticação seria uma falha de
privacidade. Agora que o `[Authorize]` existe (seção "RF-02.4" acima),
fica seguro. Duas decisões validadas antes de implementar:

1. **`GET /api/usuarios/{id}`, com checagem de propriedade** (em vez de
   `GET /api/usuarios/me`, sem id na URL). Essa é a primeira vez que o
   RF-02.4 ("identificador de outro usuário não permite consultar o
   recurso") é testado de verdade contra uma rota que recebe um id — o
   `/api/auth/me` nunca precisa checar propriedade, porque sempre devolve
   o dono do próprio token. Também resolve a pendência antiga do header
   `Location` no cadastro, já que agora existe uma URL por usuário pra
   apontar.
2. **Só os campos do cadastro**: `Id`, `Nome`, `Email`, `DataNascimento`.
   Sem `SaldoPontos` — esse campo é conceito de outro RF (RF-05/RF-06,
   pontuação), que ainda não existe; misturar agora assumiria escopo de
   uma funcionalidade futura.

**`ObterPerfilUseCase`** (`Icarus.Application/UseCases/ObterPerfil/`):
recebe `UsuarioIdAutenticado` (da claim do token) e `UsuarioIdSolicitado`
(da rota). Se forem diferentes, lança `PerfilNaoEncontradoException` —
**sem nem consultar o banco** — e a mesma exceção é lançada se o id
simplesmente não existir. As duas situações (perfil de outro usuário vs.
id inexistente) são deliberadamente indistinguíveis na resposta (`404`
genérico): não confirma a existência de uma conta que não é a do
chamador, mesmo racional de não revelar mais do que o necessário já usado
em login/refresh/logout.

**`UsuariosController`** (novo, separado do `AuthController` — "usuarios"
é um recurso diferente de "auth"). `IUsuarioRepository` ganhou
`BuscarPorIdAsync` (método novo, usado também pelo `DevController`).

**`Location` no cadastro**: `AuthController.Register` passou de
`StatusCode(201, ...)` pra `CreatedAtAction(nameof(UsuariosController.ObterPerfil), "Usuarios", new { id }, ...)`
— o ASP.NET Core monta a URL via roteamento (não é uma string hardcoded),
então ela acompanha a rota automaticamente se o caminho mudar no futuro.

**Verificação ponta a ponta** (Postgres real, `curl`): cadastro devolveu
`Location: http://localhost:5138/api/Usuarios/{id}` correto; `GET` nesse
id, autenticado, devolveu `200` com os dados certos; sem token, `401`;
com um id aleatório que não existe, `404`; e o teste mais importante —
**cadastrei um segundo usuário real** e confirmei que o primeiro,
autenticado, recebe `404` ao tentar ver o perfil do segundo (não só um id
inventado — um id de conta de verdade). Testado também em
`ObterPerfilUseCaseTests` (perfil próprio; id de outro usuário real;
id inexistente). `dotnet build` (0 avisos/erros) e `dotnet test`
(36 aprovados) revalidados.

### `DevController`: gerar token de teste sem login (2026-10-11)

A pedido do usuário: um atalho pra testar endpoints protegidos no Swagger
sem repetir cadastro+login a cada teste. **Não é uma RF do projeto** —
nenhuma regra de negócio, puramente ferramenta de desenvolvimento.

**Decisão validada antes de implementar:** o token é gerado a partir de um
`UsuarioId` de um usuário **já cadastrado** (não um GUID arbitrário sem
checagem) — assim o token corresponde a dados reais, útil quando existirem
endpoints que consultam informação do usuário (RF-03 em diante).

**`GET /api/dev/token/{usuarioId}`** (`DevController`, novo controller,
separado do `AuthController` de propósito — deixa óbvio visualmente, no
Swagger, que esse grupo de rotas não é "de produto"). Busca o usuário
(`IUsuarioRepository.BuscarPorIdAsync`, método novo) e gera só o access
token (`IJwtTokenGenerator.GerarAccessToken`) — sem checar senha, sem
gerar refresh token, sem gravar nada no banco.

**Bloqueio fora de Development é checado em runtime, não só escondido do
Swagger.** O Swagger UI já só é mapeado em `Development` (`Program.cs`),
mas a **rota** do `DevController` continua existindo nos outros ambientes
via `MapControllers()` — por isso a action injeta `IHostEnvironment` e
confere `ambiente.IsDevelopment()` como primeira linha, devolvendo `404`
(idêntico a uma rota inexistente) se não for. Esconder do Swagger sozinho
não bastaria: alguém poderia chamar a rota direto, sem nunca ter visto o
Swagger.

**Verificação ponta a ponta** (Postgres real, `curl`): cadastro → gerar
token de dev pra esse `UsuarioId` → usar esse token em `GET /api/auth/me`
devolveu o mesmo `UsuarioId` (prova que o token é válido de verdade, não
só "parece" um JWT); `UsuarioId` inexistente devolveu `404`. `dotnet
build` (0 avisos/erros) e `dotnet test` (33 aprovados) revalidados.

### Experimento revertido: `[FromForm]` no login (2026-10-10)

Por algumas horas, `POST /api/auth/login` usou `[FromForm]`
(`application/x-www-form-urlencoded`) em vez de `[FromBody]` (JSON) — a
pedido do usuário, depois de eu explicar por que o Swagger UI mostra um
textarea de JSON cru pro corpo de POST (é assim que ele renderiza qualquer
corpo `application/json`; só mostra campo por campo quando o conteúdo é
`application/x-www-form-urlencoded`/`multipart/form-data`, ou pra
parâmetros de query/rota). Era deliberadamente só nesse endpoint, como
comparação visual, nunca a intenção de virar padrão — o `[FromBody]`/JSON
é a escolha certa pra qualquer endpoint da API (é o que o `icarus-mobile`
vai consumir; senha fica mais exposta em formulário do que em JSON).

Validado funcionando nos dois formatos enquanto durou (o `ValidationFilter`
não se importa com a origem do valor, só com o objeto já resolvido pelo
model binding). Depois do teste, o próprio usuário pediu pra desfazer —
`AuthController.Login` voltou a usar `[FromBody]` (implícito), igual aos
outros endpoints. Revertido e revalidado (`dotnet build` 0 avisos/erros,
`dotnet test` 23 aprovados, login via JSON voltando a responder `200`).
`dotnet build` (0 avisos/erros) e `dotnet test` (23 aprovados) revalidados.

### RF-02.4: middleware de autenticação/autorização + endpoint de exemplo protegido

Implementado em 2026-10-10, a pedido do usuário — ele perguntou "pra que
serve ligar o `AddJwtBearer`?" e, depois de eu explicar, pediu pra
implementar na mesma conversa.

**Lado da validação** (`Icarus.Api/DependencyInjection.cs`,
`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`),
com `TokenValidationParameters` usando o **mesmo** `JwtOptions`
(emissor/audiência/chave) já configurado para o lado da emissão
(`Icarus.Infrastructure/Security/JwtTokenGenerator.cs`) — os dois lados
precisam da mesma chave, senão nenhum token emitido validaria.
`services.AddAuthorization()` registrado; `app.UseAuthentication()`
adicionado em `Program.cs`, **antes** de `app.UseAuthorization()` (ordem
importa: sem isso o `[Authorize]` nunca teria um usuário pra checar).

**Endpoint de exemplo:** `GET /api/auth/me` (`AuthController.Me`, com
`[Authorize]`) — deliberadamente **não** é o `GET` de perfil completo
(RF-01, ainda deferido): só lê o `UsuarioId` da claim `NameIdentifier` do
token (`User.FindFirstValue`) e devolve, provando que a validação está de
fato exigida e correta, sem se comprometer ainda com o formato final do
endpoint de perfil (que exige decisões próprias: quais campos expor, se
tem `Location`, etc.).

**Nome do endpoint — validado com o usuário em 2026-10-10.** A primeira
versão se chamava `eu` (tradução literal), mas isso não é convenção
nenhuma, só invenção improvisada. Duas referências reais existiam: `me`
(convenção ampla de REST/OAuth2 — é literalmente o nome do *UserInfo
Endpoint* do OIDC, e o que GitHub/Spotify/Auth0 usam para "o recurso é o
dono do token") vs. `info` (o `MapIdentityApi` do próprio ASP.NET Core,
desde o .NET 8, expõe `GET /manage/info` com esse exato propósito). Optamos
por `me`, por ser a convenção mais ampla e por manter o inglês nas partes
técnicas (seção 0) em vez de uma tradução literal do português.

**Validado (Postgres real, `curl`):** sem header `Authorization`, `401`;
com o access token devolvido pelo login, `200` com o `UsuarioId` idêntico
ao do cadastro; com o mesmo token adulterado em 1 caractere (assinatura
inválida), `401` de novo. `dotnet build` (0 avisos/erros) e `dotnet test`
(17 aprovados) revalidados — nenhum teste novo adicionado para o
`AuthController` em si (controllers finos não são testados unitariamente
neste projeto; ver "testes de integração" como pendência já documentada
acima). `Icarus.Api.http` ganhou os dois casos (sem token / com token,
encadeando o `accessToken` da resposta do login via `@name login` +
`{{login.response.body.$.accessToken}}`, recurso do REST Client).

## 4.5 Convenção de testes

**Um projeto de teste por projeto de produção**, cada um referenciando só a
camada que testa (nunca a camada de teste de outra camada, nem uma camada de
produção que não seja a sua):

```
tests/
├── Icarus.Application.UnitTests/     → testa Icarus.Application
├── Icarus.Infrastructure.UnitTests/  → testa Icarus.Infrastructure
└── Icarus.Api.UnitTests/             → testa Icarus.Api
```

Dentro de cada projeto, a estrutura de pastas **espelha o namespace da
camada correspondente** — ex.: `Icarus.Application.UnitTests/UseCases/RegisterUser/RegisterUserUseCaseTests.cs`
testa `Icarus.Application/UseCases/RegisterUser/RegisterUserUseCase.cs`. O
namespace de cada arquivo de teste segue a mesma pasta (`Icarus.Application.UnitTests.UseCases.RegisterUser`).

**Um projeto por camada só nasce quando a camada tem algo a testar** — por
isso não existe `Icarus.Domain.UnitTests` hoje (ver subseção própria abaixo).
Quando existir mais de um projeto para a mesma camada de produção, o motivo
para cada um é diferente:
- `Icarus.Application.UnitTests`: testes unitários puros, rápidos, sem nada
  externo (nem banco, nem HTTP).
- `Icarus.Infrastructure.UnitTests`: hoje só unitário (`PasswordHasher`);
  quando houver teste do `UsuarioRepository` contra Postgres de verdade, via
  Testcontainers, provavelmente vira `Icarus.Infrastructure.IntegrationTests`
  (RNF-04 exige Postgres real nesses testes, não só um banco em memória).
- `Icarus.Api.UnitTests`: hoje só o `RegisterRequestValidator` (validação da
  borda HTTP, sem subir a API); quando houver teste do `AuthController`
  ponta a ponta via `WebApplicationFactory`, provavelmente vira
  `Icarus.Api.IntegrationTests` — separado do unitário, porque integração é
  bem mais lenta.

Se tudo estivesse num projeto só, ele acumularia as dependências mais
pesadas (EF Core, Testcontainers, ASP.NET Core Test Host) mesmo para rodar
um teste rápido de validação, e não daria pra rodar "só os rápidos"
separado dos "que precisam de Postgres/Docker". É essencialmente o mesmo
padrão do template de Clean Architecture do Jason Taylor (`Application.UnitTests`,
`Application.FunctionalTests`, `Infrastructure.IntegrationTests` etc.), só
com o sufixo de tipo (`UnitTests`) já fixado desde já e `IntegrationTests`
reservado para quando o primeiro teste de integração de verdade aparecer.

**Cuidado de dependência já aplicado:** `Icarus.Application.UnitTests` usa um
`PasswordHasherFake` (interno ao projeto de teste) em vez do `PasswordHasher`
real do `Icarus.Infrastructure` — testar `Icarus.Application` não pode
depender de `Icarus.Infrastructure` (inverteria a Clean Architecture), e o
algoritmo Argon2id em si já é testado à parte, em
`Icarus.Infrastructure.UnitTests/Security/PasswordHasherTests.cs`.

#### Por que não existe `Icarus.Domain.UnitTests`

Decisão tomada com o usuário em 2026-09-27, **revertendo** uma criação feita
horas antes no mesmo dia (nunca commitada): ao reorganizar os testes,
criamos `Icarus.Domain.UnitTests` e movemos pra lá o `OcorrenciaTests`
(criado pelo Fabiam, testando `Icarus.Domain.Entities.Ocorrencia`, que
antes estava — sem pertencer a nenhuma camada testada ali — dentro de
`Icarus.Api.Tests`). Ao revisar, ficou claro que esse teste **não tem valor
real**:

```csharp
[Fact]
public void NovaOcorrenciaIniciaComoPendenteESemMovimentacoes()
{
    var ocorrencia = new Ocorrencia();

    Assert.Equal(StatusOcorrencia.Pendente, ocorrencia.Status);
    Assert.Empty(ocorrencia.MovimentacoesPontos);
}
```
Isso só confirma que os inicializadores de propriedade do C# funcionam
(`= StatusOcorrencia.Pendente`, `= new List<...>()`) — algo que a própria
linguagem garante. Não existe cenário em que esse teste pegaria um bug real;
ele só "segue" a entidade se o default mudar, sem verificar nenhuma decisão
de negócio.

**Por que isso acontece:** as entidades de `Icarus.Domain` são **POCOs
puros**, sem nenhum método ou invariante própria (decisão registrada na
seção 4.3) — validação mora no FluentValidation (API), persistência no EF
Core (Infrastructure). Hoje não existe comportamento de domínio pra testar.

**Sobre o template do Jason Taylor** (citado na seção 4.4 como referência
várias vezes): ele **tem** um `Domain.UnitTests` — mas porque o domínio dele
não é anêmico: tem uma classe `Enumeration` customizada (com lógica própria
de busca/comparação) e entidades que disparam eventos de domínio ao mudar de
estado. O `Domain.UnitTests` dele testa esse comportamento real. A
existência do projeto lá não valida ter um projeto vazio aqui — reforça o
oposto: só vale a pena quando há comportamento de verdade.

**Decisão:** removidos o projeto `Icarus.Domain.UnitTests` e o teste
`OcorrenciaTests` (mesmo raciocínio já aplicado ao `IUnitOfWork` — seção
4.4: não criar abstração/estrutura antes de haver necessidade real, YAGNI).
Quando `Icarus.Domain` ganhar comportamento de verdade (método que aplica
regra, invariante no construtor, evento de domínio), criar
`Icarus.Domain.UnitTests` naquele momento, com um teste que verifique esse
comportamento.

## 4.6 Observabilidade (logs, traces, métricas)

Fonte funcional: seção 11 ("Observabilidade, operação e manutenção") e
seção 10.2 do `01-arquitetura-da-solucao.md`, no `icarus-context`. O
documento define **o quê** logar (formato, campos, o que é proibido) mas
não escolhe **nenhuma ferramenta** — a escolha abaixo é nossa, validada com
o usuário em 2026-09-29.

### O que são as três coisas, e por que existem separadas

- **Logs** — registro de um evento pontual ("aconteceu isso, agora"). Serve
  pra investigar um caso específico depois do fato.
- **Traces** — acompanham **uma única requisição** através de várias etapas
  (Controller → Filter → UseCase → Repository → Postgres), medindo cada
  uma. Serve pra achar **onde o tempo foi gasto** dentro de uma requisição
  lenta.
- **Métricas** — números **agregados ao longo do tempo** (contagem de
  cadastros, duração média de requisição por rota). Servem pra painel e
  alerta, não pra investigar um caso específico.

A seção 10.2 do `icarus-context` já pedia um "identificador de correlação
[...] propagado aos logs" — isso é essencialmente pedir tracing, só que sem
nomear a tecnologia.

### Decisões validadas com o usuário

- **Logging: Serilog** (`Serilog.AspNetCore`), não o `Microsoft.Extensions.Logging`
  puro com `AddJsonConsole()` — Serilog é o padrão de fato da comunidade
  pra logging estruturado, com enrichers automáticos.
- **Escopo: tudo de uma vez**, incluindo tracing e métricas distribuídos via
  OpenTelemetry com exportador real — não só logging isolado.
- **Backend real desde já**: Grafana + Tempo (traces) + Prometheus
  (métricas) subidos no `icarus-infrastructure` (ver `README.md` de lá),
  em vez de adiar pra quando o RabbitMQ existir ou usar só um exportador de
  console.
- **Onde logar manualmente**: só onde uma decisão de negócio acontece
  (`RegisterUserUseCase`) ou na última linha de defesa
  (`GlobalExceptionHandler`, só para exceções **inesperadas** — as
  conhecidas, tipo `EmailJaCadastradoException`, já são logadas por quem as
  lança, logar de novo no exception handler seria duplicar a linha).
  Controller, Filter e Repository **não** ganham log manual — o que
  aconteceria ali já é coberto automaticamente: o ASP.NET Core já loga uma
  linha por requisição (`app.UseSerilogRequestLogging()`), o EF Core já loga
  os comandos SQL executados, e a instrumentação do OpenTelemetry gera
  *trace* automático de ambos (`AddAspNetCoreInstrumentation()` +
  `AddSource("Npgsql")`, este último porque o Npgsql já expõe seus próprios
  spans, bastando registrar a fonte). Log manual em toda camada é
  considerado ruído no padrão .NET, não boa prática.

### Como funciona

- **`Icarus.Api/Observability/ObservabilityExtensions.cs`**: `ConfigureSerilog`
  (chamado em `builder.Host` antes de `AddApi`) configura o Serilog —
  `ReadFrom.Configuration` (nível mínimo em `appsettings.json`, seção
  `Serilog`), `Enrich.WithSpan()` (`Serilog.Enrichers.Span` — adiciona
  `TraceId`/`SpanId` do OpenTelemetry a toda linha de log) e dois destinos
  (`WriteTo`) em paralelo: `Console` com `JsonFormatter` (JSON no stdout,
  exigência da seção 11) e `GrafanaLoki` (`Serilog.Sinks.Grafana.Loki`,
  mesmo `JsonFormatter`, endpoint do Loki) — ver subseção própria abaixo.
  `AddObservability` registra o OpenTelemetry:
  `WithTracing` (instrumentação do ASP.NET Core + fonte "Npgsql", exportador
  OTLP apontando pro Tempo) e `WithMetrics` (instrumentação do ASP.NET Core
  + do runtime do .NET, exportador OTLP apontando pro recebedor OTLP nativo
  do Prometheus — sem *scrape*, a API empurra as métricas direto).
- **Nunca logar dado pessoal**: `RegisterUserUseCase` e
  `GlobalExceptionHandler` nunca recebem o e-mail como argumento de log —
  só o `UsuarioId` (Guid, não é dado pessoal por si só) e, no erro, o
  método/rota da requisição. Reforçado (mas não garantido) pelo EF Core, que
  por padrão mascara valores de parâmetro nos logs de comando SQL
  (`@email='?'`), a menos que `EnableSensitiveDataLogging()` seja chamado —
  o que não fizemos.
- **Métodos de log com `[LoggerMessage]`**: em vez de chamar
  `logger.LogInformation(...)` direto (o que os analisadores do .NET
  marcam com `CA1848`/`CA1873` — custo de alocação/avaliação mesmo com o
  nível de log desabilitado), os logs manuais viram métodos parciais com o
  atributo `[LoggerMessage]` (`LogCadastroConcluido`, `LogEmailJaCadastrado`,
  `LogErroNaoTratado`) — código gerado em tempo de compilação, sem alocação
  extra, o padrão recomendado pelo .NET desde a versão 6.

### `icarus-infrastructure`: Grafana + Tempo + Prometheus + Loki

Quatro serviços (`compose.yaml`), configuração em `observabilidade/`:
`tempo.yaml` (recebedor OTLP/HTTP, armazenamento local), `prometheus.yml`
(mínimo, sem *scrape* — só auto-monitoramento; `--web.enable-otlp-receiver`
habilita o recebimento direto de métricas), `loki-config.yaml` (modo
*single-binary*, índice TSDB, armazenamento local), `grafana-datasources.yml`
(Tempo, Prometheus e Loki já pré-configurados como fonte de dados, sem
passo manual). Tempo publica duas portas: `4318` (recebe OTLP) e `3200`
(API de consulta — usada pelo Grafana internamente, publicada também no
host pra depuração via `curl`). Loki publica `3100` (API de *push*).

**Correlação trace ↔ log, sem indexar `TraceId` como label do Loki**: o
Tempo (`jsonData.tracesToLogsV2`) aponta pro Loki e usa
`filterByTraceID: true` — ao abrir um trace no Grafana, ele busca o texto
do `TraceId` dentro do conteúdo das linhas de log (uma busca, não um índice).
O Loki (`jsonData.derivedFields`), na direção contrária, extrai o `TraceId`
de dentro do JSON de cada linha via *regex* e vira um link pro trace
correspondente no Tempo. **Por que não indexar como label**: labels no Loki
viram um índice; `TraceId` é um valor novo a cada requisição (altíssima
cardinalidade) — torná-lo label explodiria o número de fluxos indexados e
degradaria a performance do Loki. A única label usada é `app=Icarus.Api`
(baixa cardinalidade, um valor fixo).

**Validado em 2026-09-29** (Postgres real, API via `dotnet run`, containers
de observabilidade via Docker): chamado `POST /api/auth/cadastro`; log de
sucesso (`Usuario {UsuarioId} cadastrado com sucesso`) encontrado em JSON no
stdout, com o mesmo `TraceId` das linhas de comando SQL do EF Core e da
linha de requisição do `Serilog.AspNetCore`; o trace correspondente
encontrado no Tempo (`GET /api/traces/{traceId}` na porta 3200), incluindo
o span do Npgsql com o texto da query SQL; métrica
`http_server_request_duration_seconds_count` encontrada no Prometheus,
com `http_route="api/Auth/cadastro"` e `http_response_status_code="201"`.

**Validado em 2026-10-04** (adição do Loki, a pedido do usuário): mesmas
linhas de log encontradas no Loki (`{app="Icarus.Api"} |= "<TraceId>"`),
idênticas às do stdout; provisionamento das três fontes de dados no Grafana
confirmado sem erro (`logger=provisioning.datasources ... name=Loki`).
Também observado, sem ser bug: uma tentativa de cadastro com o Postgres
fora do ar gerou `500`, e as três camadas de log (erro de conexão do EF
Core, `GlobalExceptionHandler` e o log padrão do ASP.NET Core) apareceram
corretamente correlacionadas pelo mesmo `TraceId` — confirma que o caminho
de erro também fica observável, não só o de sucesso.

### Revisão de completude (2026-10-06): 3 lacunas encontradas e corrigidas

Ao revisar a seção 4.6 contra a seção 11 do `icarus-context` linha por
linha, a pedido do usuário, três pontos ficaram de fora da primeira
implementação:

1. **Versão do serviço ausente no log.** A seção 11 pede "serviço, versão"
   — só tínhamos `Servico`. Corrigido: `ObservabilityExtensions` lê
   `Assembly.GetExecutingAssembly().GetName().Version` e adiciona como
   propriedade `Versao` em todo log (`Enrich.WithProperty`) e como
   `serviceVersion` no recurso do OpenTelemetry (`ConfigureResource`) —
   assim traces e métricas também carregam a versão, não só os logs. Hoje
   resolve para `1.0.0.0` (o `.csproj` não fixa `<Version>` explicitamente;
   ajustar isso se o projeto adotar versionamento semântico no futuro).
2. **Sem endpoint de saúde.** A seção 11 lista "saúde da API" como
   verificação mínima. Adicionado `GET /health`
   (`AspNetCore.HealthChecks.NpgSql`, via `AddHealthChecks().AddNpgSql(...)`
   em `Icarus.Api/DependencyInjection.cs`, mapeado em `Program.cs`) — responde
   `200 Healthy` só se também conseguir abrir conexão com o Postgres, não só
   "o processo está de pé". Para não afogar logs/traces com chamadas
   frequentes de orquestrador/Prometheus a esse endpoint: o
   `UseSerilogRequestLogging` rebaixa `/health` para `Debug` (abaixo do
   `MinimumLevel.Default: Information`, então nunca aparece) e
   `AddAspNetCoreInstrumentation` tem um `Filter` que exclui `/health` do
   Tempo.
3. **Retenção do Loki configurada, mas nunca aplicada.** `retention_period`
   sozinho em `limits_config` não apaga nada — faltava ligar
   `compactor.retention_enabled: true` (e, com armazenamento em filesystem,
   `compactor.delete_request_store: filesystem`, exigido pelas versões
   recentes do Loki quando a retenção está ligada). Corrigido em
   `icarus-infrastructure/observabilidade/loki-config.yaml`, e o período
   aumentado de 24h para **168h (1 semana)**, a pedido do usuário.

Validado: `GET /health` respondendo `200 Healthy`; campo `Versao` presente
em toda linha de log; chamada a `/health` **não** gerou log em
`Information` nem trace no Tempo (confirmado via busca no Loki, zero
resultados para o texto "health"); Loki reiniciado com a config nova sem
erro de validação. `dotnet build` (0 avisos/erros) e `dotnet test`
(11 aprovados) revalidados.

#### Correção adicional: botão "Logs for this span" não funcionava

Ao testar manualmente o clique de um trace no Tempo para os logs no Loki
(fluxo descrito acima, mas nunca antes clicado de fato na UI — só validado
via API), o Grafana retornava "No logs volume available". Causa: o
`tracesToLogsV2` do datasource Tempo (`grafana-datasources.yml`) só tinha
`filterByTraceID: true`, sem dizer qual *label* do Loki usar — e o Loki
exige pelo menos um seletor de stream (label) em toda consulta, não aceita
busca de texto "solta". Corrigido adicionando `customQuery: true` e um
`query` explícito com o label `app="Icarus.Api"`:
```yaml
tracesToLogsV2:
  datasourceUid: loki
  customQuery: true
  query: '{app="Icarus.Api"} |= "${__trace.traceId}"'
```
Validado consultando o Loki diretamente com essa mesma LogQL e o TraceId de
uma requisição real — retornou as 3 linhas esperadas (log do EF Core, do
`GlobalExceptionHandler` e da requisição HTTP). Lição: a verificação anterior
desta seção testou a correlação via `curl` na API do Loki, mas não o
*clique* de fato no botão da UI do Grafana — os dois não são a mesma coisa,
e só o segundo pegou esse bug.

**Ainda não implementado**: nenhum painel/dashboard próprio no Grafana
(só as fontes de dados); métricas de negócio customizadas (ex.: contador de
cadastros bem-sucedidos vs. rejeitados, além da métrica HTTP genérica que já
vem de graça da instrumentação).

## 4.7 Onboarding por IA (RabbitMQ) (2026-10-17)

Primeira integração real do `icarus-platform` com mensageria. O fluxo
completo: o mobile envia as respostas do questionário de onboarding →
a API publica um job no RabbitMQ → o worker `icarus-ai` (repositório Python
separado, branch `feature/ai-onboarding`) gera Épico + Campanha + Missões via
LLM → publica o resultado de volta → a API consome e persiste. Assíncrono
dos dois lados: a API nunca espera o worker responder na mesma requisição.

### Decisão de biblioteca: `RabbitMQ.Client` puro, não MassTransit

Validado com o usuário antes de implementar. A topologia (exchanges, filas,
routing keys, DLQ) já é de propriedade do worker Python (`aio-pika`), com
payload JSON simples — sem o envelope de mensagem que o MassTransit
normalmente impõe (headers `$type`, serialização própria). Usar o cliente
oficial dá controle total do JSON publicado/consumido, batendo exatamente
com o contrato combinado entre os dois repositórios; MassTransit brilha
quando ele é quem define a topologia e os dois lados são MassTransit, que
não é o caso aqui (interoperabilidade com um consumer não-.NET).

### Mudança no modelo de domínio (resolve divergências entre mobile/context/IA)

As entidades `Epico`, `Campanha` e `Missao` já existiam (desde a migration
inicial) mas nunca tinham sido usadas por nenhum caso de uso. Para fechar o
onboarding, o usuário revisou o modelo e resolveu três divergências que
existiam entre o que o mobile já fazia localmente (mock), o DER do
`icarus-context` e o contrato que o `icarus-ai` já tinha desenhado:

1. **Pontuação deixa de ser arbitrária/vinda da IA.** `Epico`, `Campanha` e
   `Missao` ganham `Dificuldade` (enum `Trivial`/`Facil`/`Media`/`Dificil`) e
   `Pontuacao` (int) — mas `Pontuacao` é **sempre calculada pelo backend** a
   partir da dificuldade (`Icarus.Application.Common.MapeadorPontuacao`:
   15/30/45/60), nunca aceita vinda do front ou do worker. `Campanha`
   mantém `ValorMensurado` com semântica separada (soma das missões,
   calculada ao persistir) — são dois números diferentes, para propósitos
   diferentes (recompensa fixa por dificuldade vs. progresso mensurável).
2. **`AreaDaVida` passa a ser enum (era string livre) e `Campanha` também
   ganha a sua** (copiada do Épico ao persistir — não escolhida
   independentemente). Catálogo: o das 10 áreas já validadas no RF-03.1
   (`icarus-context`, `04-requisitos.md`) — `CorpoESaude`,
   `SonoERecuperacao`, `MenteEEmocoes`, `EstudosEAprendizado`,
   `TrabalhoECarreira`, `Financas`, `RelacionamentosEComunidade`,
   `CasaEVidaPratica`, `LazerECriatividade`, `PropositoEEspiritualidade`.
   Primeira versão desta feature tinha usado um enum de 5 valores
   inventado ad-hoc (sem checar o RF-03 primeiro); corrigido no mesmo dia,
   antes de qualquer commit, a pedido do usuário ("usa o catálogo de 10
   áreas") depois que a revisão do `icarus-context` encontrou a
   divergência. `Missao` não guarda `AreaDaVida` própria; quem precisar
   dela lê via `Missao.Campanha.Epico`. RF-03.2 (área do épico imutável)
   já é respeitada por omissão — não existe nenhum caso de uso de edição
   de épico.
3. **`Priorizacao` e `Cor` saem do banco inteiramente** em `Epico` e
   `Campanha` — passam a ser responsabilidade só do front (ex.: cor
   calculada no cliente a partir da área da vida). `Campanha.EpicoId`
   continua obrigatório e `Missao.CampanhaId` continua nulável (missão
   avulsa) — nenhuma mudança aí, já era a regra certa.

Migration `AjustarEpicoCampanhaMissaoEOnboarding`: o EF Core detectou as
colunas removidas+adicionadas como **rename** (`priorizacao`→`pontuacao`,
`cor`→`dificuldade`), não drop+add — tipo-compatível (int→int, string→string)
mas semanticamente é uma coluna nova. Como as tabelas `epico`/`campanha`
ainda estavam vazias em todos os ambientes (nenhum caso de uso as usava até
agora), isso não causou perda de dado real — só vale a pena saber que a
migration *parece* um rename se alguém for lê-la depois.

### `TrabalhoOnboarding`: rastreio do job assíncrono

Nova entidade, tabela `trabalho_onboarding`. O `Id` (Guid) é o próprio
`job_id` publicado na fila — serve de correlação com o resultado assíncrono
e de chave de idempotência (um resultado duplicado, por reentrega da fila,
é ignorado se o trabalho já estiver `Concluido`). Campos: `UsuarioId`,
`Status` (`Pendente`/`Concluido`/`Falhou`), `EpicoId` (nulável, preenchido
na conclusão), `ErroMensagem` (nulável, preenchido na falha). Isso também é
a resposta prática pro "trabalhos, inbox/outbox" que a seção 18 de
`01-arquitetura-da-solucao.md` (`icarus-context`) já atribuía ao
`icarus-platform` como responsabilidade, sem nenhuma implementação até
agora (ver seção 6.3).

### Endpoints

- `POST /api/usuarios/{id}/onboarding` — `[Authorize]`, RF-02.4 (id de outro
  usuário = `404` genérico, mesmo padrão de `ObterPerfil`/`AtualizarRotina`).
  Cria o `TrabalhoOnboarding` (`Pendente`), publica o job e devolve
  **`202 Accepted`** — primeiro endpoint assíncrono do projeto; todo o resto
  até aqui era `200`/`201`/`204`. `Location` aponta pro endpoint de status.
- `GET /api/usuarios/{id}/onboarding/{jobId}` — mesmo padrão de posse.
  Devolve o status e, quando `Concluido`, o Épico com Campanha e Missões
  aninhados (evita precisar criar endpoints de leitura de Épico/Campanha/
  Missão isolados nesta rodada — eles não existem ainda, e não eram o
  pedido). Primeiro endpoint da API a serializar um enum via HTTP — por
  isso `Icarus.Api/DependencyInjection.cs` ganhou
  `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` global (ex.:
  `"concluido"`, `"facil"`, `"emAndamento"`), consistente com o camelCase
  já usado em todo o resto do JSON da API.

### Mensageria (`Icarus.Infrastructure/Messaging/`)

- `RabbitMqConnectionProvider`: uma conexão compartilhada (`AutomaticRecoveryEnabled`),
  publisher e consumer abrem canais próprios a partir dela — padrão
  recomendado pelo RabbitMQ (1 conexão, N canais).
- `RabbitMqOptions`: **`Usuario`/`Senha` são campos discretos, não uma
  connection string única** — descoberto na validação: a senha padrão do
  `icarus-infrastructure` contém um `@` literal (`urubu@100` /
  `icarus-desenvolvimento`, dependendo do `.env` local), que quebra o
  parsing de `Uri` quando embutida numa string `amqp://usuario:senha@host`
  (dois `@` na mesma string). Nomes de exchange/fila (não secretos) ficam
  em `appsettings.json`; `Usuario`/`Senha` vão em User Secrets, mesma regra
  já usada para `DefaultConnection`/`Jwt`.
- `OnboardingJobPublisher`: publica em `icarus.input` (routing key
  `onboarding`), JSON persistente (`DeliveryModes.Persistent`).
- `OnboardingResultConsumer` (`BackgroundService`): consome
  `icarus.output.onboarding`. Erro de desserialização (mensagem envenenada)
  ou tentativas esgotadas vão pra uma DLQ própria
  (`icarus.output.onboarding.dlq`) — o worker **não** configura DLQ nas
  filas de saída dele, então essa malha de segurança existe só do lado
  .NET. Retry com backoff exponencial via header `x-tentativas`
  (republish manual, mesmo mecanismo que o `icarus-ai` já usa do lado dele),
  até 3 tentativas (`RabbitMqOptions.MaxTentativas`); esgotadas, marca o
  `TrabalhoOnboarding` como `Falhou` (`MarcarOnboardingComoFalhoUseCase`)
  antes de mandar pra DLQ — sem isso, o status ficaria `Pendente` pra
  sempre e o mobile faria polling eternamente.
- Contrato JSON mapeado explicitamente (`Icarus.Infrastructure/Messaging/Contratos/`,
  `OnboardingWireMapeamento`) — sem conversor de enum automático, porque o
  formato do fio (`"FACIL"`, `"DESENVOLVIMENTO_PESSOAL"`) é combinado entre
  os dois repositórios, não uma convenção genérica do .NET.

### Validação de ponta a ponta

Com o RabbitMQ real do `icarus-infrastructure` (container já rodando) e a
API local: `POST .../onboarding` devolveu `202` com `jobId`, publicou de
verdade no `icarus.input` (confirmado sem fila de entrada vinculada ainda,
já que o worker Python não estava rodando nesta rodada — mensagem
corretamente não roteada, sem erro). Para validar o outro lado sem
depender do worker estar de pé, uma mensagem de resultado foi publicada
manualmente (API de management do RabbitMQ) em `icarus.output.onboarding`
no formato combinado — o consumer .NET processou, persistiu Épico
(`Pontuacao=30` pra `Facil`) + Campanha (`Pontuacao=45` pra `Media`,
`AreaDaVida` copiada do Épico, `ValorMensurado=30` = soma das missões) +
Missão (`Pontuacao=30`, `Horario` combinando a data corrente com o
`"07:00"` sugerido), e o `GET` de status devolveu tudo aninhado
corretamente. Fila e DLQ vazias depois (confirma `ack`, sem mensagem
perdida nem duplicada). Dados de teste limpos do Postgres depois da
validação. `dotnet build` (0 avisos) e `dotnet test` (55 aprovados,
incluindo os novos testes de `IniciarOnboardingUseCase`,
`ObterStatusOnboardingUseCase`, `PersistirResultadoOnboardingUseCase` e
`MapeadorPontuacao`) revalidados.

### Pendências desta feature

- Contrato combinado com o `icarus-ai` e documentação do `icarus-context`
  atualizados na mesma rodada (fora deste repositório — ver os próprios
  repositórios para o detalhamento).
- Integração do mobile (chamar o endpoint de verdade em vez do gerador
  local `onboardingToPlanning.js`) feita na mesma rodada, incluindo
  reconstruir a autenticação real (login/cadastro), que até então era só
  mock local no app.
- Endpoints de criação manual de Épico/Campanha/Missão (fora do onboarding)
  continuam não existindo — não eram o pedido desta vez; o modelo de dados
  já está pronto para quando isso for feito.
- Validação de ponta a ponta com o worker `icarus-ai` real rodando (em vez
  de uma mensagem injetada manualmente) ainda não foi feita nesta sessão.

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

### 6.3 Mensageria (RabbitMQ) — vive em outro repositório

O RabbitMQ (transporte assíncrono entre a API e o trabalhador Python, ver
`01-arquitetura-da-solucao.md` no `icarus-context`) **não fica neste
repositório**. A seção 18 desse documento ("Responsabilidades entre
repositórios") atribui explicitamente RabbitMQ/filas/durabilidade/rede ao
repositório `icarus-infrastructure`, enquanto o `icarus-platform` fica só com
"autorização, domínio, trabalhos, inbox/outbox e PostgreSQL" — ou seja, a API
consome a fila, mas não hospeda/configura o broker.

Decisão tomada em 2026-09-06: o container roda em
`icarus-infrastructure/compose.yaml` (`docker compose up -d rabbitmq`),
seguindo as mesmas convenções já usadas lá para o MinIO (`ICARUS_RABBITMQ_*`
com defaults, portas publicadas só em `127.0.0.1`, volume nomeado,
healthcheck). Documentação completa (portas, credenciais padrão) fica no
`README.md` do `icarus-infrastructure`, não duplicada aqui.

Hoje a API roda fora de container (`dotnet run`, seção 6.2), então ela se
conecta ao RabbitMQ do mesmo jeito que se conecta ao Postgres: via
`localhost:5672` (porta publicada no host), sem precisar de rede Docker
compartilhada entre os repositórios. Isso só passa a ser necessário quando a
API (ou o trabalhador Python do `icarus-ai`) for containerizada — nesse
momento os dois `compose` (`icarus-platform` e `icarus-infrastructure`)
precisarão declarar uma rede Docker externa compartilhada para se
comunicarem pelo nome do serviço em vez de porta publicada no host.

**Atualização 2026-10-17**: essa conexão deixou de ser hipotética — a API
publica e consome mensagens de verdade (seção 4.7, onboarding por IA).
Credenciais (`Usuario`/`Senha`) em User Secrets locais, lidas do mesmo
`.env` que o `icarus-infrastructure` já usa pra subir o container (o valor
real difere do default documentado no `compose.yaml` se alguém tiver
customizado o `.env` local — checar `docker exec <container> env | grep
RABBITMQ_DEFAULT` se a autenticação falhar).

## 7. Lacunas conhecidas / próximos passos naturais

- Modelo de dados criado (seção 4.3), mas só `Usuario` é usado por um caso de uso (cadastro); as demais entidades ainda não têm nenhum caso de uso em `Icarus.Application`.
- Observabilidade (seção 4.6): sem painel/dashboard próprio no Grafana e sem métrica de negócio customizada — só a instrumentação automática do ASP.NET Core/Npgsql por enquanto.
- RF-01 e RF-02 (cadastro e autenticação) estão completos: `POST /api/auth/cadastro` (com `Location`), `POST /api/auth/login` (com limite de 5 tentativas/15 min por conta+origem), `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me`, `GET /api/usuarios/{id}` — todos protegidos por `[Authorize]` onde cabe —, mais o `GET /health` de observabilidade (seção 4.6), o `GET /api/dev/token/{id}` (ferramenta de desenvolvimento, não é RF) e, desde 2026-10-17, `POST /api/usuarios/{id}/onboarding` + `GET /api/usuarios/{id}/onboarding/{jobId}` (onboarding por IA, seção 4.7). O resto do MVP ainda não foi iniciado. O arquivo `.http` foi removido (decisão do usuário, 2026-10-10) — o Swagger UI é hoje a única ferramenta de teste manual.
- CI não roda build/test do .NET, apenas validações estruturais (nem `dotnet format`/analisadores, apesar do `.editorconfig` já estar configurado); também não roda `dotnet ef migrations` em pipeline algum.
- Sem autenticação/autorização configurada (só `UseAuthorization()` chamado, sem esquema definido) — o DER já assume `Usuario.Id` vindo do JWT, mas isso ainda não existe na API.
- Pendências de produto herdadas do DER (não bloqueiam a estrutura, mas afetam regras futuras): estados finais da missão, escala de prioridade, regra de sobreposição de vigência da rotina, se o diário aceita múltiplas entradas por dia, estrutura definitiva dos dados externos (ATUS/Vigitel).
- A rotina diária de geração e os casos de uso para concluir, editar e inativar ocorrências ainda precisam ser implementados; o modelo persistido já suporta a regra validada na seção 4.3.
- Nulidade de vários campos novos do DER v1.1 (`Epico.AreaDaVida`, `Campanha.ValorMensurado/Unidade/ValorAtual`, `Missao.DataLimite`) foi uma interpretação da equipe, não algo explícito no documento — ver seção 4.3.
- API ainda não é containerizada. Desde 2026-10-17 ela já usa RabbitMQ de verdade (seção 4.7: publica jobs de onboarding e consome resultados do `icarus-ai`), conectando via `localhost:5672` (porta publicada pelo `icarus-infrastructure`) — ver seção 6.3. Quando a API for containerizada, será preciso criar a rede Docker compartilhada entre `icarus-platform` e `icarus-infrastructure` pra falar com o broker pelo nome do serviço.
- **Decisão pendente: estratégia de versionamento das imagens Docker.** Hoje cada serviço usa uma convenção diferente, sem termos parado para decidir isso de propósito:
  - `postgres:16` (`icarus-platform/docker-compose.yml`) — só a versão major fixada; minor/patch "flutuam" a cada `pull` novo.
  - `minio/minio:RELEASE.2025-09-07T16-13-09Z` (`icarus-infrastructure/compose.yaml`) — tag de release imutável, 100% fixada.
  - `rabbitmq:4.0-management` (`icarus-infrastructure/compose.yaml`) — major.minor fixados, patch "flutua".
  - `grafana/tempo:2.6.1`, `prom/prometheus:v3.0.1`, `grafana/grafana:11.3.0`, `grafana/loki:3.3.0` (`icarus-infrastructure/compose.yaml`) — major.minor.patch fixados, mas sem revisão periódica combinada.
  - O trade-off: tag flutuante pega correção de segurança automaticamente mas não é 100% reprodutível entre execuções/máquinas diferentes; tag imutável é reprodutível mas exige atualização manual periódica (e checar CVEs manualmente). Precisamos decidir **uma convenção única** para todos os serviços (provável candidato: tag imutável/digest para todos, seguindo o exemplo do MinIO) e aplicá-la de forma consistente. Não decidido ainda — discutir antes de adicionar novos serviços (ex.: trabalhador Python, Nginx).
- **Decisão pendente: `IUnitOfWork` deveria ser uma classe própria, em vez de `IcarusDbContext` implementar direto?** Ver seção 4.4 ("Dúvida em aberto") para as três escolas da comunidade .NET e o raciocínio de por que ficamos com a opção mais simples por enquanto. Retomar quando `SalvarAlteracoesAsync` precisar fazer mais do que chamar `SaveChangesAsync` (candidatos: eventos de domínio, padrão outbox do RabbitMQ).
- **Decisão consciente, a reavaliar: sem MediatR/CQRS, exceção em vez de Result pattern nos casos de uso.** Ver seção 4.4 ("Decisão consciente de simplificação") — divergem do padrão mais replicado hoje (ex.: template do Jason Taylor), escolhido para reduzir conceitos novos enquanto o usuário aprende. Reavaliar se o número de casos de uso ou de regras de negócio "esperadas para falhar" crescer.

## 8. Histórico de decisões e features (a atualizar conforme avançarmos)

> Esta seção será atualizada a cada nova instrução/feature implementada.

- 2026-09-08 — Regra de contexto para missões recorrentes validada: `Recorrencia`
  configura a geração, enquanto `Ocorrencia` persiste cada execução planejada.
  A entidade, enum e configuração EF Core foram adicionados ao platform;
  `MovimentacaoPontos` passou a referenciar `OcorrenciaId`; a constraint
  única `(missao_id, data_prevista)` e seus índices foram incluídos. A única
  migration `CriarModeloInicial` (inclusive designer e snapshot) foi atualizada
  diretamente, sem criar nova versão, pois o banco ainda não foi publicado.

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
- 2026-08-29 — Modelo de dados do MVP criado em `Icarus.Domain`/`Icarus.Infrastructure`
  a partir do DER em `icarus-context` (ver seção 4.3): 11 entidades, 3 enums,
  `IEntityTypeConfiguration<T>` por entidade, `EFCore.NamingConventions` para
  `snake_case`, migration `CriarModeloInicial` gerada e aplicada ao Postgres
  local. Decisão: entidades nomeadas em português (mesmos nomes do DER), como
  exceção deliberada à convenção usual de inglês, para casar com a linguagem
  ubíqua do negócio. `dotnet build`/`dotnet test` validados; tabelas conferidas
  no container via `psql`.
- 2026-08-29 — Revisão do DER a pedido do usuário: identificada lacuna real
  entre o DER e a arquitetura da solução sobre missões recorrentes (faltava
  uma entidade `Ocorrencia` — ver seção 4.3). Criada a entidade `Ocorrencia`
  com `UNIQUE(missao_id, data_prevista)`; `Missao` perdeu `Status`/`ConcluidaEm`;
  `MovimentacaoPontos` passou a referenciar `OcorrenciaId`; enum `StatusMissao`
  renomeado para `StatusOcorrencia`. Também nesta rodada: `CreatedAt`/`UpdatedAt`
  renomeados para `CriadoEm`/`AtualizadoEm` em toda a base — regra fixada pelo
  usuário de que campo de tabela é sempre nomeado em português, sem exceção
  (registrada na seção 0). Migration inicial substituída (banco de dev
  recriado do zero, sem dado real a preservar) em vez de empilhada. Adicionado
  `generated_code = true` para `Migrations/*.cs` no `.editorconfig`, evitando
  ruído dos analisadores em código gerado. `dotnet build` (0 avisos/erros) e
  `dotnet test` validados; 12 tabelas conferidas no container via `psql`.
- 2026-09-06 — Verificado, a pedido do usuário, se houve mudança no
  `icarus-context`: DER atualizado para v1.1 em 2026-09-05 (commit `7c51d46`),
  sem incorporar a entidade `Ocorrencia` que havíamos criado por conta própria
  (manteve `status`/`concluida_em` diretos em `Missao`, só acrescentando
  `CONCLUIDA_COM_ATRASO`). Decisão tomada com o usuário: **reverter** para o
  modelo oficial — `Ocorrencia` removida, `Status`/`ConcluidaEm` de volta em
  `Missao` (enum renomeado de `StatusOcorrencia` para `StatusMissao`),
  `MovimentacaoPontos` voltou a referenciar `MissaoId`. Implementados também
  os demais campos/entidade novos do DER v1.1: `Epico.AreaDaVida`/`Estado`,
  `Campanha.ValorMensurado`/`Unidade`/`ValorAtual`/`Estado`, `Missao.DataLimite`,
  `Item.ControlaApp`, e a nova entidade `MissaoIA` (auditoria de missões
  geradas por IA, com `StatusAprovacao`). Ver seção 4.3 para o detalhamento e
  as interpretações da equipe sobre nulidade de campos. A lacuna de missões
  recorrentes com histórico por ocorrência voltou a ser uma pendência de
  produto em aberto (seção 7), não mais algo resolvido na implementação.
  Migration inicial substituída de novo (banco de dev recriado do zero).
  `dotnet build` (0 avisos/erros) e `dotnet test` validados; 12 tabelas do
  modelo v1.1 conferidas no container via `psql`.
- 2026-09-06 — Container de RabbitMQ criado a pedido do usuário. Verificado no
  `icarus-context` (`01-arquitetura-da-solucao.md`, seção 18) que RabbitMQ é
  responsabilidade do repositório `icarus-infrastructure`, não do
  `icarus-platform` — só o PostgreSQL pertence a este repositório. Serviço
  `rabbitmq` (imagem `rabbitmq:4.0-management`) adicionado a
  `icarus-infrastructure/compose.yaml`, seguindo as mesmas convenções já
  usadas lá para o MinIO (env vars `ICARUS_RABBITMQ_*`, portas publicadas só
  em `127.0.0.1`, volume nomeado, healthcheck). Container validado: `healthy`,
  `rabbitmqctl status` confirmando RabbitMQ 4.0.9, portas 5672 (AMQP) e 15672
  (console administrativo) respondendo em `localhost`. Ver seção 6.3 para o
  detalhamento e a nota sobre a futura rede Docker compartilhada, necessária
  só quando a API for containerizada.
- 2026-09-06 — Iniciada a construção de cadastro/login (RF-01/RF-02).
  Passo 1 do plano: adicionado `Usuario.SenhaHash` (lacuna do DER, campo de
  senha não existia) e criada a entidade `TokenRenovacao` para o par access
  token + refresh token com rotação — decisão tomada com o usuário de
  divergir do "JWT de 24h sem renovação" do RF-02 original. Mantida a
  validação de senha do RF-01.2 como já documentada (8-128 caracteres, sem
  regra de composição), após discussão sobre NIST/OWASP recomendarem
  comprimento sobre composição. Decidido também não adotar
  `Microsoft.AspNetCore.Identity` (esquema de tabelas em inglês bateria com a
  regra de nomes em português da seção 0); autenticação será construída com
  peças padrão do .NET (`JwtBearer`, `RateLimiting`) sem o *user store* do
  `Identity`. Migration `AdicionarAutenticacao` gerada e aplicada de forma
  incremental (não substitui mais `CriarModeloInicial`, já publicada). Ver
  seção 4.4 para o detalhamento completo. `dotnet build`/`dotnet test`
  validados; 13 tabelas conferidas via `psql`.
- 2026-09-13 — Passo 2 do plano de autenticação: hash de senha com Argon2id.
  Criada a interface `IPasswordHasher` em `Icarus.Application/Abstractions`
  (primeiro código real dessa camada) e a implementação `PasswordHasher` em
  `Icarus.Infrastructure/Security`, usando o pacote
  `Konscious.Security.Cryptography.Argon2` (parâmetros conforme a segunda
  recomendação do RFC 9106), registrada via `AddSingleton` em
  `DependencyInjection.cs`. Hash serializado como string autodescritiva
  (parâmetros + salt + hash em Base64). Adicionados testes em
  `PasswordHasherTests.cs`. Ajuste de tooling: `.editorconfig` ganhou a regra
  `[tests/**/*.cs]` desligando `CA1707` (nomenclatura sem underscore) só nos
  projetos de teste, para permitir a convenção
  `Metodo_Cenario_ResultadoEsperado` nos nomes de teste. `dotnet build`
  (0 avisos/erros) e `dotnet test` (4 aprovados) validados.
- 2026-09-14 — Mesclado o trabalho do colega Fabiam (`fb994f0`, "feat:
  persistir ocorrencias recorrentes"): reintrodução da entidade `Ocorrencia`
  e do enum `StatusOcorrencia`, `MovimentacaoPontos` voltando a referenciar
  `OcorrenciaId`. Diferente da tentativa nossa em 2026-08-29, dessa vez a
  direção é validada oficialmente pelo `icarus-context` (DER v1.2, commit
  `2a2ffc7`, ADR-013) — não é mais uma interpretação própria. Decidido com o
  usuário: trazer o trabalho do colega preservando a autoria, em vez de
  reverter e reescrever como nosso.
  Duas correções feitas durante o merge: (1) a migration `CriarModeloInicial`
  tinha sido editada diretamente, apesar de já publicada (`826c46d`) —
  restaurada ao conteúdo original; o `Ocorrencia` agora entra como migration
  incremental própria (`PersistirOcorrencias`); (2) `StatusMissao` mantinha
  os valores antigos de execução, desatualizados em relação ao DER v1.2/RF-04
  (que redefinem `Missao.Status` como ciclo `Rascunho`/`Ativa`/`Inativa`,
  já que execução passa a pertencer à `Ocorrencia`) — corrigido o enum e os
  defaults (`Missao` nasce `Ativa`; `MissaoIA` nasce `Rascunho`, por ser
  sugestão ainda não aprovada). Banco local recriado do zero; 14 tabelas
  (3 migrations empilhadas: `CriarModeloInicial`, `AdicionarAutenticacao`,
  `PersistirOcorrencias`) conferidas via `psql`. `dotnet build` (0
  avisos/erros) e `dotnet test` (4 aprovados) validados.
- 2026-09-14 — Passo 3 do plano de autenticação: caso de uso de Cadastro
  (RF-01), primeiro código real em `Icarus.Application`. Criados
  `IUsuarioRepository`/`IUnitOfWork` (contratos de persistência, para a
  Application não depender do EF Core), `RegisterUserCommand`/`Result`,
  `RegisterUserValidator` (FluentValidation) e `RegisterUserUseCase`
  (normaliza e-mail, checa duplicidade, gera hash da senha).
  `EmailJaCadastradoException` para a regra de e-mail único do RF-01.1.
  `UsuarioRepository` implementado em `Icarus.Infrastructure` com EF Core;
  `IcarusDbContext` passou a implementar `IUnitOfWork`. Primeiro
  `AddApplication` (DI da camada Application), chamado em `Program.cs`.
  Pacotes adicionados: `FluentValidation`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`. Testado com
  repositório falso em memória, sem Postgres real. `dotnet build`
  (0 avisos/erros) e `dotnet test` (6 aprovados) validados.
- 2026-09-15 — Ajustes de nomenclatura a pedido do usuário: variáveis
  `contexto`/`provedor` renomeadas para `context`/`provider` em
  `UsuarioRepository`/`DependencyInjection.cs` (regra da seção 0 — fora do
  modelo de dados, inglês é o padrão; esses nomes eram referência técnica ao
  `DbContext`/`IServiceProvider`, não dado de negócio). Pasta
  `Icarus.Application/Abstractions/` renomeada para `Interfaces/` — decisão
  do usuário de padronizar o nome mais literal (usado pelo template do
  Jason Taylor) em vez do termo que a Microsoft usa em nomes de pacote
  (`Microsoft.Extensions.*.Abstractions`); ambos têm respaldo, escolhido por
  preferência. Documentada em detalhe (seção 4.4) a dúvida em aberto sobre
  `IUnitOfWork` ser ou não uma classe própria, com as três escolas da
  comunidade .NET e o critério pra retomar a decisão. `dotnet build`
  (0 avisos/erros) e `dotnet test` (6 aprovados) validados.
- 2026-09-27 — Endpoint de cadastro (`POST /api/auth/cadastro`), primeiro
  Controller da solução. Cada decisão de ferramenta/arquitetura foi validada
  com o usuário antes de codar: rota `/api/auth/cadastro` (exceção
  deliberada à regra de idioma, contra a recomendação inicial de
  `register`), DTOs HTTP próprios, `IExceptionHandler` global com
  `ProblemDetails`, filtro de validação reutilizável, mensagens pt-BR, sem
  versionamento, testes de integração adiados. A escolha do filtro
  reutilizável conflitou com o validador do Application (o filtro só
  enxerga os argumentos da action, isto é, o Request): o validador foi
  movido para a API (`RegisterRequestValidator`), `RegisterUserValidator` e
  a referência do `FluentValidation` saíram do Application. Criado o
  terceiro `DependencyInjection` (`AddApi()`). Adicionados
  `RegisterRequestValidatorTests` (12 testes no total, todos aprovados).
  Registradas as pendências: `GET` de perfil autenticado + header `Location`
  no cadastro (só após login/JWT, para não expor dados pessoais sem
  autenticação), ferramenta de teste manual (`.http` vs Scalar) e o
  trade-off de enumeração de contas do `409`. Endpoint verificado
  manualmente de ponta a ponta contra Postgres real (`curl`): 201, 409, 400
  (dados inválidos, corpo vazio, JSON malformado, data inválida) e 404
  respondendo como esperado.
- 2026-09-27 — Reorganizados os testes a pedido do usuário: de um único
  projeto (`Icarus.Api.Tests`, misturando testes de três camadas diferentes)
  para um projeto por camada de produção que tem algo a testar
  (`Icarus.Application.UnitTests`, `Icarus.Infrastructure.UnitTests`,
  `Icarus.Api.UnitTests`), com pastas espelhando o namespace dentro de cada
  um — ver seção 4.5 para a convenção completa e o porquê. Criado
  `PasswordHasherFake` em `Icarus.Application.UnitTests` para não depender de
  `Icarus.Infrastructure` num teste de `Icarus.Application`. `Icarus.slnx`
  atualizado. `dotnet build` (0 avisos/erros) e `dotnet test` (11 aprovados,
  distribuídos nos 3 projetos) validados.
  Nesta mesma rodada, criamos e depois removemos um `Icarus.Domain.UnitTests`
  (nunca commitado): ele receberia o `OcorrenciaTests` do Fabiam (testava
  `Icarus.Domain.Entities.Ocorrencia`, estava dentro de `Icarus.Api.Tests`
  sem pertencer a nenhuma camada testada ali), mas esse teste só confirma um
  inicializador de propriedade do C#, sem verificar nenhum comportamento de
  domínio — `Icarus.Domain` hoje é só POCOs, sem lógica própria. Decisão:
  remover projeto e teste (mesmo raciocínio do `IUnitOfWork`, seção 4.4:
  YAGNI, criar quando a necessidade real aparecer), documentado em detalhe
  na seção 4.5 ("Por que não existe `Icarus.Domain.UnitTests`"), incluindo
  o contraste com o `Domain.UnitTests` do template do Jason Taylor (que tem
  conteúdo real porque o domínio dele não é anêmico).
- 2026-09-29 — Observabilidade (logs, traces, métricas), a pedido do
  usuário, antes de seguir para o passo 4 do plano de autenticação. Seção
  11 do `icarus-context` já exigia formato/conteúdo dos logs (JSON no
  stdout, sem dado pessoal, com ID de correlação), mas não a ferramenta —
  escolhas validadas uma a uma: Serilog (não `AddJsonConsole` puro) para
  logging; OpenTelemetry completo (traces + métricas) desde já, não só
  logging isolado; backend real (Grafana + Tempo + Prometheus, subidos no
  `icarus-infrastructure`) em vez de exportador de console ou adiar para
  quando o RabbitMQ existir. Log manual só no `RegisterUserUseCase`
  (decisão de negócio) e no `GlobalExceptionHandler` (exceção inesperada,
  sem duplicar o log de exceções já conhecidas); Controller/Filter/Repository
  não ganham log manual — cobertos pela instrumentação automática do
  ASP.NET Core e do Npgsql. Logs convertidos para o padrão `[LoggerMessage]`
  (métodos parciais gerados em tempo de compilação), eliminando os avisos
  `CA1848`/`CA1873` dos analisadores. `Serilog.Enrichers.Span` adiciona
  `TraceId`/`SpanId` a toda linha de log, correlacionando log e trace mesmo
  sem um agregador central de logs (Loki, ainda não adicionado). Validado
  de ponta a ponta: log de sucesso do cadastro encontrado em JSON com o
  mesmo `TraceId` das linhas de SQL do EF Core; trace correspondente
  encontrado no Tempo, com o span do Npgsql mostrando a query executada;
  métrica de duração HTTP por rota/status encontrada no Prometheus. Nunca
  logamos e-mail ou outro dado pessoal — só `UsuarioId` (Guid). Ver seção
  4.6 para o detalhamento completo.
- 2026-10-04 — Adicionado o Loki (logs centralizados), a pedido do usuário,
  fechando a lacuna que a própria seção 4.6 já registrava. Novo serviço no
  `icarus-infrastructure` (`loki-config.yaml`, modo *single-binary*,
  armazenamento local); `Serilog.Sinks.Grafana.Loki` adicionado como um
  segundo destino (`WriteTo`) do Serilog, em paralelo ao stdout — nenhum dos
  dois substitui o outro. Grafana ganhou a terceira fonte de dados, com
  correlação trace↔log nos dois sentidos (Tempo aponta pro Loki via busca de
  texto pelo `TraceId`; Loki aponta pro Tempo via *regex* no JSON da linha).
  Decisão deliberada: `TraceId` nunca vira *label* do Loki (alta
  cardinalidade, degradaria a performance) — só `app=Icarus.Api`. Validado:
  logs de uma requisição de cadastro encontrados no Loki via
  `{app="Icarus.Api"} |= "<TraceId>"`, idênticos aos do stdout;
  provisionamento das 3 fontes de dados no Grafana sem erro. Também
  observado (não é bug): uma falha real de conexão com o Postgres gerou um
  500 com as três camadas de log (EF Core, `GlobalExceptionHandler`, ASP.NET
  Core) corretamente correlacionadas pelo mesmo `TraceId` — o caminho de
  erro também é observável, não só o de sucesso.
- 2026-10-06 — Revisão de completude da observabilidade (a pedido do
  usuário) encontrou e corrigiu 3 lacunas frente à seção 11 do
  `icarus-context`: versão do serviço ausente nos logs/traces/métricas
  (`Versao` adicionado via `Assembly.GetExecutingAssembly().GetName().Version`);
  ausência de `GET /health` (adicionado, checando conexão real com o
  Postgres via `AspNetCore.HealthChecks.NpgSql`, excluído do ruído de
  logs/traces); retenção do Loki configurada mas nunca aplicada (faltava
  `compactor.retention_enabled: true`; corrigido e aumentado para 1 semana).
  Ao testar manualmente o clique "Logs for this span" no Grafana (nunca
  antes clicado de fato — só validado via `curl` na API do Loki), achamos
  um quarto bug: `tracesToLogsV2` não tinha `customQuery`/`query`, então o
  Grafana não sabia qual label do Loki usar e a busca falhava. Corrigido com
  `query: '{app="Icarus.Api"} |= "${__trace.traceId}"'`. Lição: validar via
  API não substitui clicar de verdade na UI. Tudo documentado também no
  `icarus-context` (`01-arquitetura-da-solucao.md`, seção 11 ganhou o estado
  de implementação atual e o "como acessar"; ADR-014 registrado na seção 16).
- 2026-10-06 — Passo 4 do plano de autenticação: login e emissão de tokens
  (RF-02.1). Escopo validado com o usuário antes de implementar (só login +
  tokens; logout, rate limiting e o middleware de autorização ficam para os
  próximos passos). `JsonWebTokenHandler` (não o `JwtSecurityTokenHandler`
  clássico) para gerar o JWT — é o handler que o `JwtBearer` já usa por
  padrão para validar desde o .NET 8. Access token de 15 min, refresh token
  de 7 dias (ambos validados com o usuário); refresh token é uma string
  aleatória opaca, com hash SHA-256 (não Argon2id — já nasce de alta
  entropia, não precisa de derivação lenta). `LoginUseCase` trata e-mail
  inexistente e senha incorreta como a mesma `CredenciaisInvalidasException`
  (401 genérico), para não revelar se um e-mail está cadastrado. Testado em
  `LoginUseCaseTests` e `JwtTokenGeneratorTests` (o token gerado valida de
  verdade). Validado de ponta a ponta: `token_renovacao.token_hash` no banco
  difere do `refreshToken` devolvido ao cliente (confirma que só o hash foi
  persistido); as duas formas de falha de login geram a mesma resposta e o
  mesmo log, sem e-mail. `dotnet build` (0 avisos/erros) e `dotnet test`
  (17 aprovados) validados. Ver seção 4.4 ("Passo 4") para o detalhamento
  completo.
- 2026-10-10 — Ligada a interface visual (Scalar, depois trocado por Swagger
  UI a pedido do usuário no mesmo dia — Scalar documentado na seção 4.4 como
  religar depois). E, também a pedido do usuário, o middleware de
  autenticação/autorização (RF-02.4): `AddAuthentication().AddJwtBearer()` +
  `AddAuthorization()` + `app.UseAuthentication()` (antes do
  `UseAuthorization()` já existente), usando o mesmo `JwtOptions` do lado de
  emissão. Endpoint de exemplo `GET /api/auth/me` (`[Authorize]`) criado só
  para provar a validação — não é o `GET` de perfil completo, ainda
  deferido. Validado: sem token → `401`; com token válido → `200` com o
  `UsuarioId` certo; com token adulterado → `401` de novo. Botão "Authorize"
  ligado no Swagger UI (via *document/operation transformer*, já que não
  usamos `AddSwaggerGen`) e o endpoint renomeado de `eu` (tradução literal,
  sem respaldo) para `me` (convenção do UserInfo Endpoint OAuth2/OIDC) a
  pedido do usuário. Ver seção 4.4 ("RF-02.4") para o detalhamento completo.
- 2026-10-10 — `POST /api/auth/refresh`, com rotação e detecção de reuso.
  Decisão validada com o usuário antes de implementar: reuso de um refresh
  token já rotacionado (indício de token roubado) revoga **todos** os
  refresh tokens ativos do usuário, não só o reusado — recomendação do
  OAuth2 Security BCP, prática do Auth0/Okta. `IJwtTokenGenerator` ganhou
  `CalcularHashDoRefreshToken` (expõe a mesma lógica de hash já usada
  internamente); `ITokenRenovacaoRepository` ganhou `BuscarPorTokenHashAsync`
  e `RevogarTodosDoUsuarioAsync`. Rotação normal liga
  `tokenAntigo.SubstituidoPor = tokenNovo` (navegação do EF Core resolve o
  Id sozinha, sem segundo `SaveChanges`) sem marcar `RevogadoEm` — esse
  campo é só pra revogação de verdade (reuso detectado, ou futuro logout),
  conceito diferente de "foi superado pela rotação normal". Testado em
  `RefreshTokenUseCaseTests` e validado de ponta a ponta: refresh gera par
  novo; reapresentar o token original dá `401`; o token novo emitido no
  refresh **também** passa a dar `401` depois disso, confirmando que a
  sessão inteira foi revogada, não só o token reusado. `dotnet build`
  (0 avisos/erros) e `dotnet test` (23 aprovados) validados. Ver seção 4.4
  ("Refresh") para o detalhamento completo.
- 2026-10-10 — Exemplo deliberado de `[FromForm]` no `POST /api/auth/login`
  (só esse endpoint), a pedido do usuário, depois de eu explicar por que o
  Swagger UI mostra JSON cru pra `[FromBody]` e campo-a-campo só pra
  `application/x-www-form-urlencoded`. Cadastro, refresh e qualquer
  endpoint futuro continuam em JSON — isso é uma comparação isolada, não
  mudança de padrão (senha em formulário fica mais exposta em log de
  proxy do que num corpo JSON). Validado: `ValidationFilter` continua
  funcionando sem alteração (valida `ActionArguments` já resolvido, não
  importa a origem); `curl --data-urlencode` confirma login funcionando
  igual; documento OpenAPI mostra `Email`/`Senha` como propriedades
  separadas do schema só nesse endpoint. Ver seção 4.4 para o
  detalhamento completo.
- 2026-10-10 — Desfeito o experimento acima e removido o `.http`, a pedido
  do usuário ("já testei, pode desfazer. também não vamos mais usar o
  `.http`"). `AuthController.Login` voltou a `[FromBody]` (implícito),
  igual aos outros endpoints. Arquivo `src/Icarus.Api/Icarus.Api.http`
  excluído — o Swagger UI passa a ser a única ferramenta de teste manual
  do projeto. `dotnet build` (0 avisos/erros) e `dotnet test`
  (23 aprovados) revalidados; login via JSON confirmado funcionando de
  novo contra Postgres real.
- 2026-10-10 — `POST /api/auth/logout` (RF-02.2), a pedido do usuário
  ("vamos pro logout agora"). Duas decisões validadas antes de
  implementar: exige só o refresh token, sem `[Authorize]` (mesmo
  racional do refresh — evita forçar um refresh só pra poder deslogar, se
  o access token já tiver expirado); resposta sempre `204`, mesmo se o
  token já não existir/estiver expirado/revogado (RFC 7009 — não revela o
  estado do token). `LogoutUseCase` não lança nenhuma exceção e não
  devolve nada (`Task`, não `Task<T>` — não há dado útil pra devolver).
  Testado em `LogoutUseCaseTests` e validado de ponta a ponta: logout
  devolve `204`; o mesmo refresh token, usado depois num refresh, dá
  `401` (confirma que revogou de verdade); logout de novo com o mesmo
  token (idempotente) e com um token que nunca existiu também devolvem
  `204`, sem diferença. `dotnet build` (0 avisos/erros) e `dotnet test`
  (26 aprovados) validados. Ver seção 4.4 ("Logout") para o detalhamento
  completo.
- 2026-10-11 — Limite de tentativas de login (RF-02.3), a pedido do
  usuário. Antes de implementar, esclareci por que `Microsoft.AspNetCore.RateLimiting`
  (ferramenta antes só mencionada como planejada) não é o padrão real
  pra esse problema — ele limita taxa de requisição por IP/rota, não "5
  falhas por conta normalizada + origem" (não vê o corpo JSON nesse ponto
  do pipeline, conta toda requisição, não só falhas). O padrão real da
  comunidade .NET pra isso é o *lockout* do `ASP.NET Core Identity`
  (`AccessFailedCount`/`LockoutEnd`, por conta) — como não adotamos o
  Identity, construímos o equivalente na mão (`ILoginAttemptLimiter`),
  com o componente de IP adicionado. Implementação em `IMemoryCache`
  (`Icarus.Infrastructure/Security/LoginAttemptLimiter.cs`) — perda do
  contador ao reiniciar já era uma limitação aceita e documentada no
  `icarus-context` (seção 17 do `01-arquitetura-da-solucao.md`), não uma
  decisão nova. Checa o bloqueio antes de calcular o hash Argon2id;
  bloqueio responde `429` (RFC 6585), não `401` — não é uma tentativa de
  disfarce, é o código HTTP padrão pra essa situação. Testado em
  `LoginUseCaseTests` e `LoginAttemptLimiterTests`, e validado de ponta a
  ponta: 5 falhas bloqueiam a 6ª tentativa mesmo com a senha certa; outro
  usuário do mesmo IP continua logando normalmente (confirma que o
  bloqueio é por conta+origem, não só por IP). `dotnet build`
  (0 avisos/erros) e `dotnet test` (33 aprovados) validados. Ver seção 4.4
  ("Limite de tentativas de login") para o detalhamento completo.
- 2026-10-11 — `GET /api/dev/token/{usuarioId}` (`DevController`, novo), a
  pedido do usuário — atalho pra testar endpoints protegidos no Swagger
  sem repetir cadastro+login. Não é RF: gera só o access token (sem
  checar senha, sem refresh token, sem gravar nada) a partir de um
  `UsuarioId` já cadastrado (decisão validada: não um GUID arbitrário,
  pra corresponder a dados reais). Bloqueado fora de `Development` em
  runtime (`IHostEnvironment.IsDevelopment()`, não só escondido do
  Swagger — a rota continua existindo via `MapControllers()` nos outros
  ambientes e precisa recusar sozinha). Validado de ponta a ponta:
  cadastro → gera token de dev → usa no `/me`, devolve o mesmo
  `UsuarioId`; `UsuarioId` inexistente dá `404`. `dotnet build`
  (0 avisos/erros) e `dotnet test` (33 aprovados) revalidados. Ver seção
  4.4 ("DevController") para o detalhamento completo.
- 2026-10-11 — `GET /api/usuarios/{id}` (RF-01) + `Location` no cadastro,
  a pedido do usuário — a última pendência da seção 4.4, resolvida agora
  que o `[Authorize]` existe. Decisões validadas: rota recebe o id (não
  `/me` sem id), com checagem de propriedade — primeira vez que o RF-02.4
  é testado de verdade contra uma rota com id (o `/me` nunca precisa
  checar, sempre é o próprio token); só os campos do cadastro
  (`Nome`/`Email`/`DataNascimento`), sem `SaldoPontos` (é RF-05/RF-06,
  ainda não existe). `ObterPerfilUseCase` lança a mesma
  `PerfilNaoEncontradoException` (`404` genérico) tanto pra id de outro
  usuário quanto pra id inexistente — não diferencia, mesmo racional das
  outras exceções genéricas já usadas. `AuthController.Register` passou a
  usar `CreatedAtAction` (URL montada via roteamento, não hardcoded).
  Testado em `ObterPerfilUseCaseTests` e validado de ponta a ponta,
  inclusive **com um segundo usuário real cadastrado** (não só um id
  inventado): `Location` correto no cadastro; perfil próprio com `200`;
  sem token, `401`; id de outro usuário de verdade, `404`. `dotnet build`
  (0 avisos/erros) e `dotnet test` (36 aprovados) validados. Ver seção 4.4
  ("GET de perfil") para o detalhamento completo.
- 2026-10-17 — Onboarding por IA de ponta a ponta (RabbitMQ), a pedido do
  usuário ("vamos fazer TUDO o que falta fechar do fluxo"). Validado antes
  de implementar: `RabbitMQ.Client` puro, não MassTransit (topologia já é
  do worker Python, payload JSON simples). O usuário também resolveu três
  divergências de modelagem entre o mobile, o `icarus-context` e o
  `icarus-ai`: pontuação de Épico/Campanha/Missão passa a ser sempre
  calculada pelo backend a partir da dificuldade (15/30/45/60), nunca
  vinda do front/IA; `AreaDaVida` virou enum (catálogo das 10 áreas já
  validadas no RF-03.1 do `icarus-context` — a primeira versão tinha
  usado 5 valores inventados sem checar o RF-03 primeiro; corrigido no
  mesmo dia, a pedido do usuário, antes de qualquer commit) e `Campanha`
  também ganhou a sua (copiada do Épico); `Priorizacao`/`Cor` saíram do
  banco (ficam só no front). Nova entidade `TrabalhoOnboarding` rastreia o
  job assíncrono (id = job_id, serve de idempotência). Dois endpoints
  novos: `POST /api/usuarios/{id}/onboarding` (primeiro `202 Accepted` do
  projeto) e `GET .../onboarding/{jobId}`. Migration
  `AjustarEpicoCampanhaMissaoEOnboarding` aplicada (EF Core tratou as
  colunas removidas+adicionadas como rename — tabelas estavam vazias,
  sem perda de dado real). Bug real encontrado e corrigido durante a
  validação: a senha padrão do RabbitMQ no `icarus-infrastructure` contém
  um `@` literal, que quebra o parsing de `Uri` numa connection string
  única — `RabbitMqOptions` passou a ter campos discretos
  (`Host`/`Porta`/`Usuario`/`Senha`) em vez de uma string só. Validado de
  ponta a ponta contra o RabbitMQ real do `icarus-infrastructure`: job
  publicado (sem fila de entrada vinculada, pois o worker não estava
  rodando — comportamento esperado); resultado injetado manualmente via
  API de management do RabbitMQ no formato combinado, consumido e
  persistido corretamente (Épico/Campanha/Missão com pontuação calculada,
  `AreaDaVida` copiada do Épico pra Campanha, `Horario` combinando data
  corrente com o horário sugerido); fila e DLQ vazias depois (confirma
  `ack` sem perda/duplicação). Dados de teste limpos do Postgres.
  `dotnet build` (0 avisos) e `dotnet test` (55 aprovados) validados. Na
  mesma rodada, atualizados em paralelo: contrato do `icarus-ai` (fecha a
  pendência nº1 que aquele repositório já sinalizava), DER e arquitetura
  no `icarus-context`, e a integração real do mobile (reconstrução da
  autenticação + chamada real ao onboarding, substituindo o gerador local
  `onboardingToPlanning.js`) — ver os próprios repositórios. Ver seção 4.7
  para o detalhamento completo.
