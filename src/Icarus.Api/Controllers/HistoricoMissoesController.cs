using System.Security.Claims;
using Icarus.Api.Contracts.HistoricoMissoes;
using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/historico/missoes")]
public sealed class HistoricoMissoesController(ConsultarHistoricoMissoesUseCase consultar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaHistoricoMissoes>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid usuarioId, [FromQuery] ConsultarHistoricoMissoesRequest r, CancellationToken ct)
        => Ok(await consultar.ExecuteAsync(new(Autenticado(), usuarioId, r.Inicio, r.Fim, r.Estado, r.CampanhaId, r.Avulsas, r.Limite, r.Cursor), ct));

    [HttpGet("campanhas")]
    [ProducesResponseType<IReadOnlyList<CampanhaHistoricoMissao>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Campanhas(Guid usuarioId, CancellationToken ct)
        => Ok(await consultar.ListarCampanhasAsync(Autenticado(), usuarioId, ct));

    [HttpGet("{ocorrenciaId:long}")]
    [ProducesResponseType<DetalheHistoricoMissao>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detalhe(Guid usuarioId, long ocorrenciaId, CancellationToken ct)
        => Ok(await consultar.ConsultarAsync(Autenticado(), usuarioId, ocorrenciaId, ct));

    private Guid Autenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
