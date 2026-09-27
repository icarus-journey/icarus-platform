using Icarus.Api.Contracts.Auth;
using Icarus.Application.UseCases.RegisterUser;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(RegisterUserUseCase registerUser) : ControllerBase
{
    /// <summary>Cadastra um novo usuário (RF-01). A validação do corpo é feita pelo ValidationFilter.</summary>
    [HttpPost("cadastro")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Nome, request.Email, request.Senha, request.DataNascimento);

        var result = await registerUser.ExecuteAsync(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(result.UsuarioId));
    }
}
