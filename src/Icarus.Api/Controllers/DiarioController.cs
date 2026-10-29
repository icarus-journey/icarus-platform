using System.Security.Claims;
using Icarus.Api.Contracts.Diario;
using Icarus.Application.UseCases.AtualizarEntradaDiario;
using Icarus.Application.UseCases.CriarEntradaDiario;
using Icarus.Application.UseCases.ExcluirEntradaDiario;
using Icarus.Application.UseCases.ListarEntradasDiario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/diario")]
public sealed class DiarioController(ListarEntradasDiarioUseCase listar, CriarEntradaDiarioUseCase criar,
    AtualizarEntradaDiarioUseCase atualizar, ExcluirEntradaDiarioUseCase excluir) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<EntradaDiarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid usuarioId, CancellationToken cancellationToken)
    {
        var entradas = await listar.ExecuteAsync(new(Autenticado(), usuarioId), cancellationToken);
        return Ok(entradas.Select(EntradaDiarioResponse.DoResultado).ToList());
    }

    [HttpPost]
    [ProducesResponseType<EntradaDiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Criar(Guid usuarioId, EntradaDiarioRequest request, CancellationToken cancellationToken)
    {
        var entrada = await criar.ExecuteAsync(new(Autenticado(), usuarioId, request.Tipo!.Value,
            request.DataRegistro, request.Texto), cancellationToken);
        return CreatedAtAction(nameof(Listar), new { usuarioId }, EntradaDiarioResponse.DoResultado(entrada));
    }

    [HttpPatch("{entradaId:long}")]
    [ProducesResponseType<EntradaDiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid usuarioId, long entradaId, EntradaDiarioRequest request,
        CancellationToken cancellationToken)
    {
        var entrada = await atualizar.ExecuteAsync(new(Autenticado(), usuarioId, entradaId, request.Tipo!.Value,
            request.DataRegistro, request.Texto), cancellationToken);
        return Ok(EntradaDiarioResponse.DoResultado(entrada));
    }

    [HttpDelete("{entradaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid usuarioId, long entradaId, CancellationToken cancellationToken)
    {
        await excluir.ExecuteAsync(new(Autenticado(), usuarioId, entradaId), cancellationToken);
        return NoContent();
    }

    private Guid Autenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
