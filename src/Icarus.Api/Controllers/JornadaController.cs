using System.Security.Claims;
using Icarus.Application.UseCases.ConsultarJornada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[ApiController, Authorize]
[Route("api/usuarios/{id:guid}/jornada")]
public sealed class JornadaController(ConsultarJornadaUseCase consultar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ConsultarJornadaResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
        => Ok(await consultar.ExecuteAsync(new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id), ct));
}
