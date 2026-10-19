using System.Security.Claims;
using Icarus.Api.Contracts.Missoes;
using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Application.UseCases.ListarMissoesAvulsas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/missoes")]
public class MissoesController(
    CriarMissaoUseCase criarMissao,
    AtualizarMissaoUseCase atualizarMissao,
    ListarMissoesAvulsasUseCase listarMissoesAvulsas) : ControllerBase
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
            r.Id, null, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite, r.Horario, r.Status))
            .ToList());
    }

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
        var command = new CriarMissaoCommand(
            usuarioIdAutenticado, usuarioId, request.CampanhaId, request.Titulo, request.Descricao,
            request.Beneficio, request.Dificuldade, request.DataLimite, request.Horario);

        var resultado = await criarMissao.ExecuteAsync(command, cancellationToken);
        var response = ParaResponse(resultado);

        return CreatedAtAction(nameof(Atualizar), new { usuarioId, missaoId = resultado.Id }, response);
    }

    /// <summary>Edita uma missão (RF-01) — não altera a campanha dona nem o status.</summary>
    [Authorize]
    [HttpPatch("{missaoId:long}")]
    [ProducesResponseType<MissaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid usuarioId, long missaoId, AtualizarMissaoRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new AtualizarMissaoCommand(
            usuarioIdAutenticado, usuarioId, missaoId, request.Titulo, request.Descricao,
            request.Beneficio, request.Dificuldade, request.DataLimite, request.Horario);

        var resultado = await atualizarMissao.ExecuteAsync(command, cancellationToken);

        return Ok(ParaResponse(resultado));
    }

    private Guid ObterUsuarioIdAutenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static MissaoResponse ParaResponse(CriarMissaoResult r) => new(
        r.Id, r.CampanhaId, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite, r.Horario, r.Status);

    private static MissaoResponse ParaResponse(AtualizarMissaoResult r) => new(
        r.Id, r.CampanhaId, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite, r.Horario, r.Status);
}
