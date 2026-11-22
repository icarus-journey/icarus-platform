using System.Security.Claims;
using Icarus.Api.Contracts.Suporte;
using Icarus.Application.Suporte;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/suporte/relatos")]
public sealed class SuporteController(ISuporte suporte) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ResumoRelato>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Enviar(CriarRelatoRequest request, CancellationToken cancelamento)
    {
        var relato = await suporte.EnviarAsync(Autenticado(), request.ChaveEnvio,
            request.Assunto.Trim(), request.Descricao.Trim(), cancelamento);
        return CreatedAtAction(nameof(Consultar), new { relato.Id }, relato);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ResumoRelato>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Listar(CancellationToken cancelamento) =>
        Ok(await suporte.ListarAsync(Autenticado(), cancelamento));

    [HttpGet("{id:long}")]
    [ProducesResponseType<DetalheRelato>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Consultar(long id, CancellationToken cancelamento)
    {
        var relato = await suporte.ConsultarAsync(Autenticado(), id, cancelamento);
        return relato is null ? NotFound() : Ok(relato);
    }

    private Guid Autenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
