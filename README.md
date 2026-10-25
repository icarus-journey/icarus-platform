# Icarus Platform

Contém a API, o processamento assíncrono, as regras de negócio e a persistência
dos dados transacionais do Icarus.

Loja autenticada: [`05-contrato-da-loja.md`](../icarus-context/docs/implementation-design/platform/05-contrato-da-loja.md)
(RF-06.1 a RF-06.4). Configure o preço da proteção por `Loja__CustoProtecao`
(padrão: 500 pontos). O consumo automático diário é uma entrega separada.

Perfil autenticado: [`04-contrato-de-autenticacao.md`](../icarus-context/docs/implementation-design/platform/04-contrato-de-autenticacao.md).
A consulta existente inclui estudo/trabalho opcionais; `PUT /api/usuarios/{id}/perfil`
atualiza esses quatro campos e `PUT /api/usuarios/{id}/fuso` mantém o fuso IANA.
Cadastro e saldo permanecem protegidos por contratos separados.

## Diário autenticado

`/api/usuarios/{usuarioId}/diario` permite listar (`GET`), criar (`POST`),
editar (`PATCH /{entradaId}`) e excluir logicamente (`DELETE /{entradaId}`).
Tipo (`reflexao`/`deslize`), data e texto de até 10.000 caracteres compõem a
entrada. A migration `20261025022507_PersistirEntradasDiario` acrescenta tipo e
data de exclusão, preservando o texto existente. A autorização ocorre no caso
de uso e na consulta por titular. Diário não altera pontos nem entra na IA.
Contrato e limites: [Diário](../icarus-context/docs/implementation-design/platform/08-contrato-do-diario.md).

Os testes de Diário usam `ICARUS_TESTES_POSTGRES` para criar/remover um banco
isolado; verificam persistência, isolamento, exclusão e concorrência.

## Consumo analítico

Referências públicas Gold 2.0.0 são lidas somente do snapshot PostgreSQL vigente
em `dados_publicos`; a API não executa ETL. HTTP autenticado e isolado por titular (referências/relatório 1.0.0,
progresso 1.1.0): `GET /api/usuarios/{id}/referencias-publicas` (área e tipo de dia),
`GET /api/usuarios/{id}/progresso` (períodos completos) e relatório semanal
`GET /api/usuarios/{id}/relatorios/semanais/atual`, `POST .../semanais` e
`POST .../semanais/tentar-novamente`. Consulte os contratos
[Gold/mobile](../icarus-context/docs/implementation-design/platform/06-consumo-analitico-mobile-api.md)
e [relatório](../icarus-context/docs/implementation-design/platform/07-relatorio-semanal-v1.md).

A migration `AdicionarRelatorioSemanalConfiavel` cria trabalho, inbox/outbox e
unicidade por período. RabbitMQ e `icarus-ai` geram a síntese real; consultas
reutilizam o resultado persistido. Sem fuso ou registros, não geram conteúdo.
Previstas/taxa, sequência, comparação completa e cenários continuam dependentes
de origens ainda não implementadas; ausência não vira zero.

[Contratos JSON e exemplos RF-10](contracts/relatorio-semanal/v1/) são versionados
em conjunto com a cópia independente do trabalhador. Os exemplos são massa de
teste e não conteúdo demonstrativo do aplicativo. Rode `dotnet test`; para as
integrações PostgreSQL, forneça `ICARUS_TESTES_POSTGRES` com uma conexão local
capaz de criar bancos temporários. Cada fixture usa banco isolado e o remove.

O progresso 1.1.0 acrescenta `evolucao`: `granularidade` (`dia`, `semana`, `mes`)
e pontos com `inicio`, `fim`, `concluidas`. Semana usa dias; mês usa blocos de
sete dias desde o primeiro dia, com último bloco parcial; seis meses usa meses
civis. Série e total usam o mesmo conjunto de realizações no fuso do titular.
Sem conclusões, a série é ausente; zeros internos indicam datas sem conclusão
registrada, sem afirmar ausência de atividade. É extensão apenas da leitura
pessoal HTTP: a mensagem RF-10 e a lista permitida da IA permanecem 1.0.0.
