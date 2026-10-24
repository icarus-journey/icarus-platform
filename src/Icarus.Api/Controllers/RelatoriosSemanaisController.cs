using System.Security.Claims;
using Icarus.Application.UseCases.ConsultarRelatorioSemanal;
using Icarus.Application.UseCases.SolicitarRelatorioSemanal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[ApiController, Authorize]
[Route("api/usuarios/{id:guid}/relatorios/semanais")]
public sealed class RelatoriosSemanaisController(ConsultarRelatorioSemanalUseCase consultar,
    SolicitarRelatorioSemanalUseCase solicitar) : ControllerBase
{
    private Guid Titular => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
