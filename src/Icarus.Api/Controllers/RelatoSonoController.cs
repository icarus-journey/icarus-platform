using System.Security.Claims;
using Icarus.Api.Contracts.Usuarios;
using Icarus.Application.UseCases.RelatoSono;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios/{id:guid}/relato-sono")]
public sealed class RelatoSonoController(RelatoSonoUseCase casoDeUso) : ControllerBase
{
    private Guid Autenticado => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType<RelatoSonoResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
        => Ok(await casoDeUso.ConsultarAsync(Autenticado, id, ct));

    // Enviar ambos os valores nulos remove o relato; a versão impede sobrescrita de outra edição.
    [HttpPut]
    [ProducesResponseType<RelatoSonoResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarRelatoSonoRequest pedido, CancellationToken ct)
        => Ok(await casoDeUso.AtualizarAsync(new(Autenticado, id, pedido.HabitualMinutos, pedido.MetaMinutos, pedido.Versao), ct));
}
