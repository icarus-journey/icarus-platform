using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="TokenRenovacao"/>. A implementação concreta (EF Core)
/// fica em Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface ITokenRenovacaoRepository
{
    Task AdicionarAsync(TokenRenovacao tokenRenovacao, CancellationToken cancellationToken);

    /// <summary>Usado no refresh (RF-02) para localizar o token pelo hash recebido.</summary>
    Task<TokenRenovacao?> BuscarPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Marca como revogado todo refresh token ainda ativo do usuário. Usado ao detectar
    /// reuso de um token já rotacionado (indício de roubo) — reage revogando a sessão
    /// inteira, não só o token reusado, forçando login novo em todos os dispositivos.
    /// </summary>
    Task RevogarTodosDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);
}
