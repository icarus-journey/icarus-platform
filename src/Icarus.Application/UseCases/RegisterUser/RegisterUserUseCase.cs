using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;

namespace Icarus.Application.UseCases.RegisterUser;

/// <summary>Caso de uso do RF-01 (cadastro). Assume que o comando já foi validado pelo chamador.</summary>
public sealed class RegisterUserUseCase(
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
{
    public async Task<RegisterUserResult> ExecuteAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        if (await usuarioRepository.ExisteComEmailAsync(email, cancellationToken))
        {
            throw new EmailJaCadastradoException(email);
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = command.Nome.Trim(),
            Email = email,
            SenhaHash = passwordHasher.Hash(command.Senha),
            DataNascimento = command.DataNascimento
        };

        await usuarioRepository.AdicionarUsuarioAsync(usuario, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return new RegisterUserResult(usuario.Id);
    }
}
