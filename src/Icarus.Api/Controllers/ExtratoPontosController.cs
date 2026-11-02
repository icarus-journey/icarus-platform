using System.Security.Claims;
using Icarus.Api.Contracts.ExtratoPontos;
using Icarus.Application.UseCases.ConsultarExtratoPontos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/extrato")]
public sealed class ExtratoPontosController(ConsultarExtratoPontosUseCase consultar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaExtratoPontos>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid usuarioId, [FromQuery] ConsultarExtratoPontosRequest r, CancellationToken ct)
        => Ok(await consultar.ExecuteAsync(new(Autenticado(), usuarioId, r.Inicio, r.Fim, r.TodoHistorico, r.Natureza, r.Limite, r.Cursor), ct));

    [HttpGet("{movimentoId:long}")]
    [ProducesResponseType<DetalheExtratoPontos>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalhe(Guid usuarioId, long movimentoId, CancellationToken ct)
        => Ok(await consultar.ConsultarAsync(Autenticado(), usuarioId, movimentoId, ct));

    private Guid Autenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
