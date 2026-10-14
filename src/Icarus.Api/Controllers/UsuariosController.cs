using System.Security.Claims;
using Icarus.Api.Contracts.Usuarios;
using Icarus.Application.UseCases.ObterPerfil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/[controller]")]
public class UsuariosController(ObterPerfilUseCase obterPerfil) : ControllerBase
{
    /// <summary>
    /// Perfil do usuário (RF-01). Exige <c>[Authorize]</c> e o id da rota tem que ser o
    /// mesmo do token (RF-02.4) — id de outro usuário devolve <c>404</c>, igual a um id
    /// que não existe, pra não confirmar a existência de uma conta que não é a sua.
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPerfil(Guid id, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ObterPerfilCommand(usuarioIdAutenticado, id);

        var result = await obterPerfil.ExecuteAsync(command, cancellationToken);

        return Ok(new UsuarioResponse(result.Id, result.Nome, result.Email, result.DataNascimento));
    }
}
