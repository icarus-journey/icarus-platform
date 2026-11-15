using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.Logout;

/// <summary>
/// Caso de uso do logout (RF-02.2): revoga o refresh token informado. Segue a RFC 7009
/// (revogação de token OAuth2) — sempre "funciona" do ponto de vista do chamador, mesmo
/// se o token já não existir, já estiver expirado ou já revogado, para não revelar o
/// estado do token a quem pede o logout. Não exige o access token: o próprio refresh
/// token já é a credencial (mesmo racional do refresh).
/// </summary>
public sealed partial class LogoutUseCase(
    ITokenRenovacaoRepository tokenRenovacaoRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtTokenGenerator,
    ILogger<LogoutUseCase> logger)
{
    public async Task ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenGenerator.CalcularHashDoRefreshToken(command.RefreshToken);
        var token = await tokenRenovacaoRepository.BuscarPorTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.RevogadoEm is not null)
        {
            // Nada a fazer — o resultado desejado ("esse token não funciona mais") já é
            // verdade. Não loga nem lança nada: não é um evento, nem um erro.
            return;
        }

        token.RevogadoEm = DateTimeOffset.UtcNow;
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogLogoutConcluido(token.UsuarioId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Usuario {UsuarioId} deslogado — refresh token revogado")]
    private partial void LogLogoutConcluido(Guid usuarioId);
}
