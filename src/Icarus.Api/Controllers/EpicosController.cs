using Icarus.Application.UseCases.ExcluirEpico;
using System.Security.Claims;
using Icarus.Api.Contracts.Epicos;
using Icarus.Application.UseCases.AtualizarEpico;
using Icarus.Application.UseCases.ConcluirEpico;
using Icarus.Application.UseCases.CriarEpico;
using Icarus.Application.UseCases.ListarEpicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/epicos")]
public class EpicosController(
    ExcluirEpicoUseCase excluirEpico,
    CriarEpicoUseCase criarEpico,
    AtualizarEpicoUseCase atualizarEpico,
    ListarEpicosUseCase listarEpicos,
    ConcluirEpicoUseCase concluirEpico) : ControllerBase
{
    /// <summary>Lista os épicos do usuário, com campanhas e missões aninhadas (RF-01.5).</summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType<List<EpicoComCampanhasResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new ListarEpicosCommand(usuarioIdAutenticado, usuarioId);

        var resultado = await listarEpicos.ExecuteAsync(command, cancellationToken);

        return Ok(resultado.Select(ParaResponse).ToList());
    }

    /// <summary>Cria um épico manualmente (RF-01), fora do onboarding por IA.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType<EpicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Criar(Guid usuarioId, CriarEpicoRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new CriarEpicoCommand(
            usuarioIdAutenticado, usuarioId, request.Titulo, request.Descricao,
            request.AreaDaVida, request.Dificuldade, request.PrazoInicio, request.PrazoFim);

        var resultado = await criarEpico.ExecuteAsync(command, cancellationToken);

        var response = new EpicoResponse(
            resultado.Id, resultado.Titulo, resultado.Descricao, resultado.AreaDaVida,
            resultado.Dificuldade, resultado.Pontuacao, resultado.PrazoInicio, resultado.PrazoFim, resultado.Estado);

        return CreatedAtAction(nameof(Listar), new { usuarioId }, response);
    }

    /// <summary>Edita um épico (RF-01) — não altera área da vida nem estado.</summary>
    [Authorize]
    [HttpPatch("{epicoId:long}")]
    [ProducesResponseType<EpicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid usuarioId, long epicoId, AtualizarEpicoRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new AtualizarEpicoCommand(
            usuarioIdAutenticado, usuarioId, epicoId, request.Titulo, request.Descricao,
            request.Dificuldade, request.PrazoInicio, request.PrazoFim);

        var resultado = await atualizarEpico.ExecuteAsync(command, cancellationToken);

        return Ok(new EpicoResponse(
            resultado.Id, resultado.Titulo, resultado.Descricao, resultado.AreaDaVida,
            resultado.Dificuldade, resultado.Pontuacao, resultado.PrazoInicio, resultado.PrazoFim, resultado.Estado));
    }

    /// <summary>
    /// Conclui um épico manualmente (RF-03.8) — irreversível e não altera o estado das
    /// campanhas. Idempotente: concluir de novo um épico já concluído é um no-op.
    /// </summary>
    [Authorize]
    [HttpPost("{epicoId:long}/concluir")]
    [ProducesResponseType<EpicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Concluir(Guid usuarioId, long epicoId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new ConcluirEpicoCommand(usuarioIdAutenticado, usuarioId, epicoId);

        var resultado = await concluirEpico.ExecuteAsync(command, cancellationToken);

        return Ok(new ConcluirEpicoResponse(resultado.Id, resultado.Estado));
    }

    /// <summary>Exclusão lógica com preservação do histórico e dos pontos. Repetição segura.</summary>
    [Authorize]
    [HttpDelete("{epicoId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid usuarioId, long epicoId, CancellationToken ct)
    {
        await excluirEpico.ExecuteAsync(new(ObterUsuarioIdAutenticado(), usuarioId, epicoId), ct);
        return NoContent();
    }

    private Guid ObterUsuarioIdAutenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static EpicoComCampanhasResponse ParaResponse(EpicoListadoResult epico) => new(
        epico.Id, epico.Titulo, epico.Descricao, epico.AreaDaVida, epico.Dificuldade, epico.Pontuacao,
        epico.PrazoInicio, epico.PrazoFim, epico.Estado,
        epico.Campanhas.Select(campanha => new CampanhaComMissoesResponse(
            campanha.Id, epico.Id, campanha.Titulo, campanha.Descricao, campanha.AreaDaVida, campanha.Dificuldade,
            campanha.Pontuacao, campanha.PrazoInicio, campanha.PrazoFim, campanha.ValorMensurado,
            campanha.Unidade, campanha.ValorAtual, campanha.Estado,
            campanha.Missoes.Select(missao => new MissaoResumoResponse(
                missao.Id, missao.Titulo, missao.Descricao, missao.Beneficio, missao.Dificuldade,
                missao.Pontuacao, missao.DataLimite, missao.Horario, missao.Status, missao.ConcluidaEm,
                missao.Recorrente))
                .ToList()))
            .ToList());
}
