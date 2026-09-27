# Icarus Platform — Documentação do Projeto

> Documento vivo. Mantido e atualizado à medida que novas decisões e features forem implementadas.
> Última atualização: 2026-09-27

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
Autenticação (cadastro/login) está em construção — ver seção 4.4. O único
endpoint exposto hoje é o de cadastro (`POST /api/auth/cadastro`).

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
- Único endpoint hoje: `POST /api/auth/cadastro`. O `Icarus.Api.http` continua sendo o template residual (referencia `/weatherforecast/`, que não existe mais) — ver decisão pendente sobre ferramenta de teste na seção 4.4.
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

**Ferramenta para testar a API manualmente: arquivo `.http`.** Não há um
padrão único da comunidade: o `.http` é o padrão de primeira parte (o
template oficial já gera o `Icarus.Api.http`; roda no Visual Studio, VS Code
e Rider sem pacote extra) e, para interface visual, o Swagger UI
(Swashbuckle) foi o padrão por anos mas saiu dos templates no .NET 9 — a
documentação oficial de OpenAPI cita Swagger UI, Scalar e Redoc, e o Scalar
é o substituto mais adotado. Seguimos com o `.http` (sem dependência nova,
versionado no Git como roteiro repetível; o `Icarus.Api.http` foi reescrito
com os casos do cadastro e cresce com login/refresh/logout). **Scalar
(`Scalar.AspNetCore`) fica como opção futura**, se quisermos explorar a API
visualmente: mapear a página só em Development e ligar `launchBrowser` +
`launchUrl` em `launchSettings.json` (hoje `launchBrowser: false`, padrão do
template .NET 9+) para o Visual Studio abri-la no F5. Os dois podem coexistir.

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

**Ainda não implementado** (próximos passos do plano em andamento): geração
de JWT, casos de uso e endpoints de login/refresh/logout, `JwtBearer` +
rate limiting de login, `GET` de perfil e `Location` (pendência acima),
testes de integração do endpoint.

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
API (ou o trabalhador Python do `icarus-data`) for containerizada — nesse
momento os dois `compose` (`icarus-platform` e `icarus-infrastructure`)
precisarão declarar uma rede Docker externa compartilhada para se
comunicarem pelo nome do serviço em vez de porta publicada no host.

## 7. Lacunas conhecidas / próximos passos naturais

- Modelo de dados criado (seção 4.3), mas só `Usuario` é usado por um caso de uso (cadastro); as demais entidades ainda não têm nenhum caso de uso em `Icarus.Application`.
- Só existe um endpoint (`POST /api/auth/cadastro`); login, refresh, logout e o `GET` de perfil (com `Location` no cadastro) ainda não existem — ver seção 4.4. O arquivo `.http` continua residual do template.
- CI não roda build/test do .NET, apenas validações estruturais (nem `dotnet format`/analisadores, apesar do `.editorconfig` já estar configurado); também não roda `dotnet ef migrations` em pipeline algum.
- Sem autenticação/autorização configurada (só `UseAuthorization()` chamado, sem esquema definido) — o DER já assume `Usuario.Id` vindo do JWT, mas isso ainda não existe na API.
- Pendências de produto herdadas do DER (não bloqueiam a estrutura, mas afetam regras futuras): estados finais da missão, escala de prioridade, regra de sobreposição de vigência da rotina, se o diário aceita múltiplas entradas por dia, estrutura definitiva dos dados externos (ATUS/Vigitel).
- A rotina diária de geração e os casos de uso para concluir, editar e inativar ocorrências ainda precisam ser implementados; o modelo persistido já suporta a regra validada na seção 4.3.
- Nulidade de vários campos novos do DER v1.1 (`Epico.AreaDaVida`, `Campanha.ValorMensurado/Unidade/ValorAtual`, `Missao.DataLimite`) foi uma interpretação da equipe, não algo explícito no documento — ver seção 4.3.
- API ainda não é containerizada nem usa RabbitMQ (broker existe no `icarus-infrastructure`, mas nenhum outbox/inbox foi implementado aqui ainda — ver seção 6.3). Quando a API for containerizada, será preciso criar a rede Docker compartilhada entre `icarus-platform` e `icarus-infrastructure`.
- **Decisão pendente: estratégia de versionamento das imagens Docker.** Hoje cada serviço usa uma convenção diferente, sem termos parado para decidir isso de propósito:
  - `postgres:16` (`icarus-platform/docker-compose.yml`) — só a versão major fixada; minor/patch "flutuam" a cada `pull` novo.
  - `minio/minio:RELEASE.2025-09-07T16-13-09Z` (`icarus-infrastructure/compose.yaml`) — tag de release imutável, 100% fixada.
  - `rabbitmq:4.0-management` (`icarus-infrastructure/compose.yaml`) — major.minor fixados, patch "flutua".
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
