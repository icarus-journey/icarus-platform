using Icarus.Application.UseCases.ConsultarMissao;
using Icarus.Application.UseCases.ExcluirMissao;
using System.Security.Claims;
using Icarus.Api.Contracts.Missoes;
using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Application.UseCases.InativarMissao;
using Icarus.Application.UseCases.ListarMissoesAvulsas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/missoes")]
public class MissoesController(
    ConsultarMissaoUseCase consultarMissao,
    ExcluirMissaoUseCase excluirMissao,
    CriarMissaoUseCase criarMissao,
    AtualizarMissaoUseCase atualizarMissao,
    ListarMissoesAvulsasUseCase listarMissoesAvulsas,
    ConcluirMissaoUseCase concluirMissao,
    InativarMissaoUseCase inativarMissao) : ControllerBase
{
    /// <summary>
    /// Lista as missões avulsas do usuário (sem campanha, RF-03). Missões atreladas a uma
    /// campanha vêm aninhadas em <c>GET /usuarios/{id}/epicos</c>, não aqui.
    /// </summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType<List<MissaoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new ListarMissoesAvulsasCommand(usuarioIdAutenticado, usuarioId);

        var resultado = await listarMissoesAvulsas.ExecuteAsync(command, cancellationToken);

        return Ok(resultado.Select(r => new MissaoResponse(
            r.Id, null, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite, r.Horario, r.Status, r.ConcluidaEm, r.Recorrente))
            .ToList());
    }

    /// <summary>Consulta a configuração vigente e sua versão para edição (HTTP 1.1.0).</summary>
    [Authorize]
    [HttpGet("{missaoId:long}")]
    [ProducesResponseType<EdicaoMissaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid usuarioId, long missaoId, CancellationToken ct)
        => Ok(EdicaoMissaoResponse.DoResultado(await consultarMissao.ExecuteAsync(new(ObterUsuarioIdAutenticado(), usuarioId, missaoId), ct)));

    /// <summary>Cria uma missão manualmente (RF-01), com ou sem campanha (RF-03).</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType<MissaoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Criar(Guid usuarioId, CriarMissaoRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var recorrencia = request.Recorrencia is { } tipo
            ? new RecorrenciaCommandInfo(tipo, request.DiasSemana, request.DataFimRecorrencia)
            : null;
        var command = new CriarMissaoCommand(
            usuarioIdAutenticado, usuarioId, request.CampanhaId, request.Titulo, request.Descricao,
            request.Beneficio, request.Dificuldade, request.DataLimite, request.Horario, recorrencia);

        var resultado = await criarMissao.ExecuteAsync(command, cancellationToken);
        var response = ParaResponse(resultado);

        return CreatedAtAction(nameof(Atualizar), new { usuarioId, missaoId = resultado.Id }, response);
    }

    /// <summary>Edita uma missão (RF-01) — não altera a campanha dona nem o status.</summary>
    [Authorize]
    [HttpPatch("{missaoId:long}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<EdicaoMissaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid usuarioId, long missaoId, AtualizarMissaoRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new AtualizarMissaoCommand(
            usuarioIdAutenticado, usuarioId, missaoId, request.Titulo, request.Descricao,
            request.Beneficio, request.Dificuldade, request.DataLimite, request.Horario,
            request.Recorrencia, request.DiasSemana, request.DataFimRecorrencia, request.DiaDoMes, request.VersaoEsperada);

        var resultado = await atualizarMissao.ExecuteAsync(command, cancellationToken);

        return Ok(EdicaoMissaoResponse.DoResultado(resultado));
    }

    /// <summary>
    /// Conclui uma missão (RF-04.6). Credita pontos ao usuário e, se a missão pertencer a uma
    /// campanha mensurável, soma à evolução do valor atual dela. Idempotente: concluir de novo
    /// uma missão já concluída não credita pontos de novo.
    /// </summary>
    [Authorize]
    [HttpPost("{missaoId:long}/concluir")]
    [ProducesResponseType<ConcluirMissaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Concluir(Guid usuarioId, long missaoId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new ConcluirMissaoCommand(usuarioIdAutenticado, usuarioId, missaoId);

        var resultado = await concluirMissao.ExecuteAsync(command, cancellationToken);

        return Ok(new ConcluirMissaoResponse(
            resultado.Id, resultado.ConcluidaEm, resultado.ConcluidaComAtraso, resultado.PontuacaoCreditada, resultado.SaldoPontosAtual));
    }

    /// <summary>
    /// Para uma missão recorrente (RF-04.6) — ela deixa de gerar novas ocorrências. Cancela as
    /// ocorrências pendentes ainda não vencidas; preserva as já vencidas como histórico, sem
    /// permitir concluí-las depois. Só missão recorrente pode ser inativada. Idempotente.
    /// </summary>
    [Authorize]
    [HttpPost("{missaoId:long}/inativar")]
    [ProducesResponseType<InativarMissaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Inativar(Guid usuarioId, long missaoId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new InativarMissaoCommand(usuarioIdAutenticado, usuarioId, missaoId);

        var resultado = await inativarMissao.ExecuteAsync(command, cancellationToken);

        return Ok(new InativarMissaoResponse(resultado.Id, resultado.Status, resultado.OcorrenciasCanceladas));
    }

    /// <summary>Exclusão lógica com preservação do histórico e dos pontos. Repetição segura.</summary>
    [Authorize]
    [HttpDelete("{missaoId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid usuarioId, long missaoId, CancellationToken ct)
    {
        await excluirMissao.ExecuteAsync(new(ObterUsuarioIdAutenticado(), usuarioId, missaoId), ct);
        return NoContent();
    }

    private Guid ObterUsuarioIdAutenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static MissaoResponse ParaResponse(CriarMissaoResult r) => new(
        r.Id, r.CampanhaId, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite, r.Horario, r.Status, r.ConcluidaEm, r.Recorrente);

}
