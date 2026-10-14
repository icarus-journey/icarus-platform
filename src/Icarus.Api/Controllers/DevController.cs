using Icarus.Api.Contracts.Dev;
using Icarus.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>
/// Endpoints exclusivos de desenvolvimento — nenhum deles é uma RF do projeto; servem
/// só pra testar outras rotas manualmente (ex.: colar um token no Swagger sem repetir
/// cadastro+login a cada teste). Bloqueados fora de <c>Development</c> em runtime
/// (checagem dentro da action, não só a rota ficar escondida do Swagger — o Swagger em
/// si já só é mapeado em Development, mas a rota continua existindo nos outros
/// ambientes e precisa recusar sozinha).
/// </summary>
[ApiController]
[Route("api/dev")]
public class DevController(IUsuarioRepository usuarioRepository, IJwtTokenGenerator jwtTokenGenerator, IHostEnvironment ambiente) : ControllerBase
{
    /// <summary>
    /// Gera um access token válido para um usuário já cadastrado, sem checar senha.
    /// Nunca cria usuário nem gera refresh token — só o access token, pra testar
    /// endpoints protegidos por <c>[Authorize]</c>.
    /// </summary>
    [HttpGet("token/{usuarioId:guid}")]
    [ProducesResponseType<DevTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GerarToken(Guid usuarioId, CancellationToken cancellationToken)
    {
        if (!ambiente.IsDevelopment())
        {
            return NotFound();
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return NotFound();
        }

        var (accessToken, accessTokenExpiraEm) = jwtTokenGenerator.GerarAccessToken(usuario.Id);

        return Ok(new DevTokenResponse(accessToken, accessTokenExpiraEm));
    }
}
