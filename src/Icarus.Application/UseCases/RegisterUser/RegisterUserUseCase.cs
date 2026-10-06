using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.RegisterUser;

/// <summary>Caso de uso do RF-01 (cadastro). Assume que o comando já foi validado pelo chamador.</summary>
public sealed partial class RegisterUserUseCase(
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<RegisterUserUseCase> logger)
{
    public async Task<RegisterUserResult> ExecuteAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        // Nunca logar o e-mail (ou qualquer outro dado pessoal) — seção 11 da arquitetura no icarus-context.
        if (await usuarioRepository.ExisteComEmailAsync(email, cancellationToken))
        {
            LogEmailJaCadastrado();
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

        LogCadastroConcluido(usuario.Id);

        return new RegisterUserResult(usuario.Id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de cadastro rejeitada: e-mail já cadastrado")]
    private partial void LogEmailJaCadastrado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Usuario {UsuarioId} cadastrado com sucesso")]
    private partial void LogCadastroConcluido(Guid usuarioId);
}
