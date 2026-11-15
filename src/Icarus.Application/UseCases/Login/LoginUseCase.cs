using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.Login;

/// <summary>Caso de uso do RF-02.1 (login). Assume que o comando já foi validado pelo chamador.</summary>
public sealed partial class LoginUseCase(
    IUsuarioRepository usuarioRepository,
    ITokenRenovacaoRepository tokenRenovacaoRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ILoginAttemptLimiter loginAttemptLimiter,
    ILogger<LoginUseCase> logger)
{
    public async Task<LoginResult> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        // RF-02.3: checa o limite antes de tudo — nem calcula o hash Argon2id se já
        // estiver bloqueado, evitando custo de CPU desnecessário.
        if (loginAttemptLimiter.EstaBloqueado(email, command.Origem))
        {
            LogBloqueadoPorLimiteDeTentativas();
            throw new LimiteDeTentativasExcedidoException();
        }

        // Nunca logar o e-mail (ou qualquer outro dado pessoal) — seção 11 da arquitetura no icarus-context.
        var usuario = await usuarioRepository.BuscarPorEmailAsync(email, cancellationToken);

        if (usuario is null || !passwordHasher.Verify(command.Senha, usuario.SenhaHash))
        {
            loginAttemptLimiter.RegistrarFalha(email, command.Origem);
            LogFalhaDeLogin();
            throw new CredenciaisInvalidasException();
        }

        loginAttemptLimiter.RegistrarSucesso(email, command.Origem);

        var (accessToken, accessTokenExpiraEm) = jwtTokenGenerator.GerarAccessToken(usuario.Id);

        var (refreshToken, refreshTokenHash, refreshTokenExpiraEm) = jwtTokenGenerator.GerarRefreshToken();

        await tokenRenovacaoRepository.AdicionarAsync(new TokenRenovacao
        {
            UsuarioId = usuario.Id,
            TokenHash = refreshTokenHash,
            ExpiraEm = refreshTokenExpiraEm
        }, cancellationToken);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogLoginConcluido(usuario.Id);

        return new LoginResult(accessToken, accessTokenExpiraEm, refreshToken, refreshTokenExpiraEm);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login bloqueado temporariamente: limite de tentativas excedido")]
    private partial void LogBloqueadoPorLimiteDeTentativas();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de login rejeitada: credenciais inválidas")]
    private partial void LogFalhaDeLogin();

    [LoggerMessage(Level = LogLevel.Information, Message = "Usuario {UsuarioId} autenticado com sucesso")]
    private partial void LogLoginConcluido(Guid usuarioId);
}
