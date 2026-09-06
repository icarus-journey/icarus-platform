# Icarus Platform — Documentação do Projeto

> Documento vivo. Mantido e atualizado à medida que novas decisões e features forem implementadas.
> Última atualização: 2026-09-06

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
modelo de dados completo do MVP (12 entidades, ver seção 4.3) já foram
criados. Ainda não há casos de uso, controllers nem nenhuma feature de negócio
(regras de aplicação) codificada — a API não expõe nenhum endpoint funcional.

O modelo de dados segue o DER do `icarus-context` na versão **1.1**
(2026-09-05) — ver seção 4.3 para o histórico de reconciliação entre nossa
implementação e as atualizações do DER.

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

- `Icarus.Domain`: sem dependências (camada mais interna). Contém as 12 entidades do modelo de dados e os enums (ver seção 4.3).
- `Icarus.Application`: referencia `Icarus.Domain`. Ainda vazio (sem casos de uso).
- `Icarus.Infrastructure`: referencia `Icarus.Application`. Contém `DependencyInjection.cs`, o `DbContext`, as configurações EF Core e as migrations.
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
- Sem dependências (nem de EF Core) — entidades são POCOs puros, mapeamento fica todo em `Icarus.Infrastructure`.
- `Entities/`: as 12 entidades do modelo de dados (ver seção 4.3).
- `Common/EntidadeBase.cs`: classe abstrata com `CriadoEm`/`AtualizadoEm`, herdada por todas as entidades exceto `MovimentacaoPontos` (que só tem `CriadoEm`, por ser um registro imutável). Fica em `Common/` (não em `Entities/`) seguindo o padrão dos templates de Clean Architecture .NET mais adotados pela comunidade (Jason Taylor, Ardalis): abstrações compartilhadas do domínio (bases, futuras interfaces de evento de domínio etc.) ficam separadas das entidades concretas.
- `Enums/`: `StatusMissao`, `EstadoEpico`, `EstadoCampanha`, `StatusAprovacaoMissaoIA`, `ControlaApp`, `TipoRelatorio`, `TipoMovimentacaoPontos` — listas iniciais sugeridas pelo DER, ainda decisões de domínio em aberto.

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
- `Missao.Status`, `MissaoIA.Status`/`StatusAprovacao`, `Epico.Estado`, `Campanha.Estado`, `Item.ControlaApp`, `Relatorio.Tipo`, `MovimentacaoPontos.Tipo`: enums C# persistidos como texto (`HasConversion<string>()`), não como inteiro — mais legível direto no banco e mais fácil de estender.
- `Campanha.ValorMensurado`/`ValorAtual`: `decimal?` (`numeric(12,2)`) — nulo quando a campanha não é mensurável.

**Entidades criadas** (namespace `Icarus.Domain.Entities`): `Usuario`,
`Rotina`, `Epico`, `Campanha`, `Missao`, `MissaoIA`, `Recorrencia`, `Diario`,
`Relatorio`, `Item`, `InventarioItem`, `MovimentacaoPontos` — as 12 do DER v1.1.

#### Revisão do DER v1.0 (2026-08-29): lacuna da entidade `Ocorrencia` — revertida em 2026-09-06

Ao revisar o DER v1.0 contra o documento de arquitetura mais amplo
(`01-arquitetura-da-solucao.md`, seções 8.1, 8.2, 9.2 e 9.4), foi identificada
uma aparente inconsistência: a arquitetura menciona "ocorrência única por
missão e data prevista" e "conclusão única por ocorrência", enquanto o DER só
tinha `MISSAO` com um único `status`/`concluida_em` — o que não representa uma
missão recorrente com histórico por data. Como correção, criamos uma entidade
`Ocorrencia` própria (não documentada no `icarus-context`), com `Missao`
perdendo `Status`/`ConcluidaEm` e `MovimentacaoPontos` passando a referenciar
`OcorrenciaId`.

**Em 2026-09-05, quem mantém o `icarus-context` publicou o DER v1.1**
(commit `7c51d46`, "docs: adiciona ajustes no DER e contextos") **sem**
incorporar essa entidade: `MISSAO` continua com `status`/`concluida_em`
diretos, e o único ajuste relacionado foi acrescentar o valor de status
`CONCLUIDA_COM_ATRASO`. Ou seja, a equipe do `icarus-context` seguiu evoluindo
o modelo original (status único por missão) sem ciência da nossa mudança.

**Decisão tomada com o usuário em 2026-09-06:** reverter para o modelo do DER
oficial. `Ocorrencia` foi removida; `Missao` voltou a ter `Status`
(`StatusMissao`, com `Pendente`/`Concluida`/`Cancelada`/`ConcluidaComAtraso`) e
`ConcluidaEm` diretos; `MovimentacaoPontos` voltou a referenciar `MissaoId`. A
lacuna de missões recorrentes com histórico por ocorrência **continua sem
solução física no DER** — é uma pendência do produto, não mais algo que
resolvemos por conta própria na implementação. Deve ser levantada com quem
mantém o `icarus-context` antes de qualquer feature que dependa disso (ex.:
concluir/desfazer uma data específica de uma missão recorrente).

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
- `inventario_item(usuario_id, item_id)` único (uma linha por item no inventário);
- `CHECK` de não-negatividade em `usuario.saldo_pontos`, `missao.pontuacao`, `missao_ia.pontuacao`, `item.valor`, `inventario_item.quantidade`;
- índices de consulta em `rotina(usuario_id, vigencia_inicio, vigencia_fim)`, `missao(usuario_id/campanha_id/horario/status)`, `missao_ia(usuario_id/status_aprovacao)`, `movimentacao_pontos(usuario_id, criado_em)`, entre outros listados no DER.

**Exclusão em cascata:** todas as relações a partir de `Usuario` (e as
compostas `Epico → Campanha`, `Campanha → Missao`, `Campanha → MissaoIA`,
`Missao → Recorrencia`, `Item → InventarioItem`) usam `ON DELETE CASCADE`,
coerente com a decisão do MVP de exclusão física simples (seção 29 do DER).
**Exceções deliberadas:**
- as FKs opcionais de `MovimentacaoPontos` para `Item` e `Missao` usam
  `ON DELETE RESTRICT` — excluir um item ou uma missão não pode apagar o
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

Como nenhum dado real existe ainda, a migration inicial foi **substituída**
outra vez em 2026-09-06 (banco local derrubado com `dotnet ef database drop` e
recriado do zero) em vez de empilhar migrations incrementais em cima da versão
com `Ocorrencia` — mesma decisão de manter o histórico de migrations limpo
enquanto não há dado de produção.

Validado em 2026-09-06: as 12 tabelas do modelo v1.1 (`usuario`, `rotina`,
`epico`, `campanha`, `missao`, `missao_ia`, `recorrencia`, `diario`,
`relatorio`, `item`, `inventario_item`, `movimentacao_pontos`) foram criadas
no container (`docker exec icarus-postgres psql -U postgres -d icarus -c "\dt"`),
com `dotnet build` (0 avisos, 0 erros) e `dotnet test` passando limpos.
`Migrations/*.cs` continua marcado como `generated_code = true` no
`.editorconfig` para os analisadores do .NET não gerarem ruído em código que
nunca é editado à mão.

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

- Modelo de dados criado (seção 4.3), mas nenhum caso de uso/serviço em `Icarus.Application` ainda usa essas entidades.
- Nenhum Controller em `Icarus.Api` (arquivo `.http` residual referencia endpoint inexistente).
- CI não roda build/test do .NET, apenas validações estruturais (nem `dotnet format`/analisadores, apesar do `.editorconfig` já estar configurado); também não roda `dotnet ef migrations` em pipeline algum.
- Sem autenticação/autorização configurada (só `UseAuthorization()` chamado, sem esquema definido) — o DER já assume `Usuario.Id` vindo do JWT, mas isso ainda não existe na API.
- Pendências de produto herdadas do DER (não bloqueiam a estrutura, mas afetam regras futuras): estados finais da missão, escala de prioridade, regra de sobreposição de vigência da rotina, se o diário aceita múltiplas entradas por dia, estrutura definitiva dos dados externos (ATUS/Vigitel).
- **Missões recorrentes não têm status por ocorrência no DER oficial** — `Missao` tem um único `Status`/`ConcluidaEm`, então não há hoje como saber se a ocorrência de segunda foi concluída e a de quarta não (ver seção 4.3, "Revisão do DER v1.0"). Levantar com quem mantém o `icarus-context` antes de implementar conclusão de missão recorrente.
- Nulidade de vários campos novos do DER v1.1 (`Epico.AreaDaVida`, `Campanha.ValorMensurado/Unidade/ValorAtual`, `Missao.DataLimite`) foi uma interpretação da equipe, não algo explícito no documento — ver seção 4.3.

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
