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

## Consumo analítico

Referências públicas Gold 2.0.0 são lidas somente do snapshot PostgreSQL vigente
em `dados_publicos`; a API não executa ETL. HTTP 1.0.0 autenticado e isolado por
titular: `GET /api/usuarios/{id}/referencias-publicas` (área e tipo de dia),
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
