using System.Security.Claims;
using Icarus.Api.Contracts.Auth;
using Icarus.Application.UseCases.Login;
using Icarus.Application.UseCases.Logout;
using Icarus.Application.UseCases.RefreshToken;
using Icarus.Application.UseCases.RegisterUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(RegisterUserUseCase registerUser, LoginUseCase login, RefreshTokenUseCase refreshToken, LogoutUseCase logout) : ControllerBase
{
    /// <summary>
    /// Cadastra um novo usuário (RF-01). A validação do corpo é feita pelo ValidationFilter.
    /// <c>Location</c> aponta pro `GET` de perfil do usuário recém-criado (pendência
    /// antiga, resolvida agora que o perfil existe e exige autenticação).
    /// </summary>
    [HttpPost("cadastro")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Nome, request.Email, request.Senha, request.DataNascimento);

        var result = await registerUser.ExecuteAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(UsuariosController.ObterPerfil),
            "Usuarios",
            new { id = result.UsuarioId },
            new RegisterResponse(result.UsuarioId));
    }

    /// <summary>
    /// Autentica um usuário (RF-02.1). A validação do corpo é feita pelo ValidationFilter.
    /// RF-02.3: após 5 falhas em 15 min para a mesma combinação de e-mail + IP, responde
    /// <c>429</c> em vez de tentar a senha.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var origem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecida";
        var command = new LoginCommand(request.Email, request.Senha, origem);

        var result = await login.ExecuteAsync(command, cancellationToken);

        return Ok(new LoginResponse(result.AccessToken, result.AccessTokenExpiraEm, result.RefreshToken, result.RefreshTokenExpiraEm));
    }

    /// <summary>
    /// Troca um refresh token por um par novo de tokens (RF-02, com rotação). A
    /// validação do corpo é feita pelo ValidationFilter. Reusa <see cref="LoginResponse"/>
    /// — mesmo formato de saída, sem motivo de negócio para serem tipos diferentes.
    /// </summary>o 
    [HttpPost("refresh")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var result = await refreshToken.ExecuteAsync(command, cancellationToken);

        return Ok(new LoginResponse(result.AccessToken, result.AccessTokenExpiraEm, result.RefreshToken, result.RefreshTokenExpiraEm));
    }

    /// <summary>
    /// Revoga um refresh token (RF-02.2). Reusa <see cref="RefreshRequest"/> — mesmo
    /// formato de entrada do refresh, sem motivo de negócio pra serem tipos diferentes.
    /// Sempre <c>204</c>, mesmo se o token já não existir/estiver expirado/revogado
    /// (RFC 7009: não revela o estado do token a quem pede o logout). Não exige o
    /// access token — o refresh token em si já é a credencial, mesmo racional do refresh.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);

        await logout.ExecuteAsync(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Endpoint de exemplo (RF-02.4): só responde com um access token válido no header
    /// <c>Authorization: Bearer</c>. Não é o `GET` de perfil completo — só prova que o
    /// `AddJwtBearer` está exigindo e validando o token, e que o `UsuarioId` lido das
    /// claims é o correto. Nome "me" segue a convenção do UserInfo Endpoint do
    /// OAuth2/OIDC e de APIs como GitHub/Spotify (seção 4.4 do PROJETO.md).
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return Ok(new MeResponse(usuarioId));
    }
}
