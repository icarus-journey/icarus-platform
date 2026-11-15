using System.Security.Claims;
using Icarus.Api.Contracts.HistoricoRelatorios;
using Icarus.Application.UseCases.ConsultarHistoricoRelatorios;
using Icarus.Application.UseCases.ConsultarRelatorioSemanal;
using Icarus.Application.UseCases.SolicitarRelatorioSemanal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[ApiController, Authorize]
[Route("api/usuarios/{id:guid}/relatorios/semanais")]
public sealed class RelatoriosSemanaisController(ConsultarRelatorioSemanalUseCase consultar,
    SolicitarRelatorioSemanalUseCase solicitar, ConsultarHistoricoRelatoriosUseCase historico) : ControllerBase
{
    private Guid Titular => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType<PaginaHistoricoRelatorios>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid id, [FromQuery] ConsultaHistoricoRelatoriosRequest r, CancellationToken ct)
        => Ok(await historico.ExecuteAsync(new(Titular, id, r.Limite, r.Cursor), ct));

    [HttpGet("{relatorioId:long}")]
    [ProducesResponseType<DetalheRelatorioSalvo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Detalhe(Guid id, long relatorioId, CancellationToken ct)
        => Ok(await historico.ConsultarAsync(Titular, id, relatorioId, ct));

    [HttpGet("atual")]
    [ProducesResponseType<ConsultarRelatorioSemanalResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
        => Ok(await consultar.ExecuteAsync(new(Titular, id), ct));

    [HttpPost]
    [ProducesResponseType<ConsultarRelatorioSemanalResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ConsultarRelatorioSemanalResult>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Solicitar(Guid id, CancellationToken ct) => Iniciar(id, false, ct);

    [HttpPost("tentar-novamente")]
    [ProducesResponseType<ConsultarRelatorioSemanalResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ConsultarRelatorioSemanalResult>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> TentarNovamente(Guid id, CancellationToken ct) => Iniciar(id, true, ct);

    private async Task<IActionResult> Iniciar(Guid id, bool novaTentativa, CancellationToken ct)
    {
        var resultado = await solicitar.ExecuteAsync(new(Titular, id, novaTentativa), ct);
        return resultado.Estado == "processando" ? AcceptedAtAction(nameof(Consultar), new { id }, resultado) : Ok(resultado);
    }
}
