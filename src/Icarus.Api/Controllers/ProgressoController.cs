using System.Security.Claims;
using Icarus.Api.Contracts.Analise;
using Icarus.Application.UseCases.ConsultarProgresso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[ApiController, Authorize]
[Route("api/usuarios/{id:guid}/progresso")]
public sealed class ProgressoController(ConsultarProgressoUseCase consultar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ConsultarProgressoResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid id, [FromQuery] ConsultarProgressoRequest request, CancellationToken ct)
    {
        var autenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await consultar.ExecuteAsync(new(autenticado, id, request.Periodo), ct));
    }
}
