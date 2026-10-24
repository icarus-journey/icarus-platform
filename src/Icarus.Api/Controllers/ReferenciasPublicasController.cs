using System.Security.Claims;
using Icarus.Api.Contracts.Analise;
using Icarus.Application.UseCases.ConsultarReferenciasPublicas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios/{id:guid}/referencias-publicas")]
public sealed class ReferenciasPublicasController(ConsultarReferenciasPublicasUseCase consultar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ConsultarReferenciasPublicasResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid id, [FromQuery] ConsultarReferenciasPublicasRequest request, CancellationToken ct)
    {
        var autenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await consultar.ExecuteAsync(new(autenticado, id, request.AreaDaVida, request.TipoDia), ct));
    }
}
