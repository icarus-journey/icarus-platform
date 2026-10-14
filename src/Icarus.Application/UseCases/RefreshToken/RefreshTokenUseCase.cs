using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.RefreshToken;

/// <summary>
/// Caso de uso do refresh (RF-02), com rotação: cada uso de um refresh token gera um
/// par novo e marca o antigo como substituído. Reuso de um token já substituído é
/// tratado como indício de roubo — revoga todos os tokens ativos do usuário.
/// </summary>
public sealed partial class RefreshTokenUseCase(
    ITokenRenovacaoRepository tokenRenovacaoRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtTokenGenerator,
    ILogger<RefreshTokenUseCase> logger)
{
    public async Task<RefreshTokenResult> ExecuteAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenGenerator.CalcularHashDoRefreshToken(command.RefreshToken);
        var tokenExistente = await tokenRenovacaoRepository.BuscarPorTokenHashAsync(tokenHash, cancellationToken);

        if (tokenExistente is null)
        {
            LogRefreshTokenInvalido();
            throw new RefreshTokenInvalidoException();
        }

        if (tokenExistente.SubstituidoPorId is not null)
        {
            // Esse token já foi trocado por outro antes — alguém está reapresentando um
            // refresh token velho. Reage revogando a sessão inteira, não só esse token.
            LogReusoDetectado(tokenExistente.UsuarioId);
            await tokenRenovacaoRepository.RevogarTodosDoUsuarioAsync(tokenExistente.UsuarioId, cancellationToken);
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
            throw new RefreshTokenInvalidoException();
        }

        if (tokenExistente.RevogadoEm is not null || tokenExistente.ExpiraEm <= DateTimeOffset.UtcNow)
        {
            LogRefreshTokenInvalido();
            throw new RefreshTokenInvalidoException();
        }

        var (accessToken, accessTokenExpiraEm) = jwtTokenGenerator.GerarAccessToken(tokenExistente.UsuarioId);
        var (refreshToken, refreshTokenHash, refreshTokenExpiraEm) = jwtTokenGenerator.GerarRefreshToken();

        var tokenNovo = new TokenRenovacao
        {
            UsuarioId = tokenExistente.UsuarioId,
            TokenHash = refreshTokenHash,
            ExpiraEm = refreshTokenExpiraEm
        };

        tokenExistente.SubstituidoPor = tokenNovo;

        await tokenRenovacaoRepository.AdicionarAsync(tokenNovo, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogRefreshConcluido(tokenExistente.UsuarioId);

        return new RefreshTokenResult(accessToken, accessTokenExpiraEm, refreshToken, refreshTokenExpiraEm);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de refresh rejeitada: token inválido, expirado ou revogado")]
    private partial void LogRefreshTokenInvalido();

    [LoggerMessage(Level = LogLevel.Error, Message = "Reuso de refresh token detectado para o usuário {UsuarioId} — todos os tokens revogados")]
    private partial void LogReusoDetectado(Guid usuarioId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refresh token rotacionado com sucesso para o usuário {UsuarioId}")]
    private partial void LogRefreshConcluido(Guid usuarioId);
}
