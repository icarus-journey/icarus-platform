using System.Security.Claims;
using Icarus.Api.Contracts.Campanhas;
using Icarus.Application.UseCases.AtualizarCampanha;
using Icarus.Application.UseCases.ConcluirCampanha;
using Icarus.Application.UseCases.CriarCampanha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/campanhas")]
public class CampanhasController(
    CriarCampanhaUseCase criarCampanha,
    AtualizarCampanhaUseCase atualizarCampanha,
    ConcluirCampanhaUseCase concluirCampanha) : ControllerBase
{
    /// <summary>Cria uma campanha manualmente (RF-01), sempre atrelada a um épico do usuário.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType<CampanhaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Criar(Guid usuarioId, CriarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new CriarCampanhaCommand(
            usuarioIdAutenticado, usuarioId, request.EpicoId, request.Titulo, request.Descricao,
            request.Dificuldade, request.PrazoInicio, request.PrazoFim, request.ValorMensurado, request.Unidade);

        var resultado = await criarCampanha.ExecuteAsync(command, cancellationToken);
        var response = ParaResponse(resultado);

        return CreatedAtAction(nameof(Atualizar), new { usuarioId, campanhaId = resultado.Id }, response);
    }

    /// <summary>Edita uma campanha (RF-01) — não altera área da vida, estado nem o épico dono.</summary>
    [Authorize]
    [HttpPatch("{campanhaId:long}")]
    [ProducesResponseType<CampanhaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid usuarioId, long campanhaId, AtualizarCampanhaRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new AtualizarCampanhaCommand(
            usuarioIdAutenticado, usuarioId, campanhaId, request.Titulo, request.Descricao,
            request.Dificuldade, request.PrazoInicio, request.PrazoFim, request.ValorMensurado, request.Unidade);

        var resultado = await atualizarCampanha.ExecuteAsync(command, cancellationToken);

        return Ok(ParaResponse(resultado));
    }

    /// <summary>
    /// Conclui uma campanha manualmente (RF-03.7) — confirmação explícita; atingir o valor
    /// mensurado não conclui por si só. Idempotente: concluir de novo é um no-op.
    /// </summary>
    [Authorize]
    [HttpPost("{campanhaId:long}/concluir")]
    [ProducesResponseType<ConcluirCampanhaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Concluir(Guid usuarioId, long campanhaId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = ObterUsuarioIdAutenticado();
        var command = new ConcluirCampanhaCommand(usuarioIdAutenticado, usuarioId, campanhaId);

        var resultado = await concluirCampanha.ExecuteAsync(command, cancellationToken);

        return Ok(new ConcluirCampanhaResponse(resultado.Id, resultado.Estado));
    }

    private Guid ObterUsuarioIdAutenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static CampanhaResponse ParaResponse(CriarCampanhaResult r) => new(
        r.Id, r.EpicoId, r.Titulo, r.Descricao, r.AreaDaVida, r.Dificuldade, r.Pontuacao,
        r.PrazoInicio, r.PrazoFim, r.ValorMensurado, r.Unidade, r.ValorAtual, r.Estado);

    private static CampanhaResponse ParaResponse(AtualizarCampanhaResult r) => new(
        r.Id, r.EpicoId, r.Titulo, r.Descricao, r.AreaDaVida, r.Dificuldade, r.Pontuacao,
        r.PrazoInicio, r.PrazoFim, r.ValorMensurado, r.Unidade, r.ValorAtual, r.Estado);
}
