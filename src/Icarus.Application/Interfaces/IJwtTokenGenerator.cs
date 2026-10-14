namespace Icarus.Application.Interfaces;

/// <summary>
/// Geração dos dois tokens do RF-02: o access token (JWT, curto) e o refresh
/// token (opaco, longo, com rotação — ver TokenRenovacao). A implementação
/// concreta (criptografia) fica em Icarus.Infrastructure.
/// </summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiraEm) GerarAccessToken(Guid usuarioId);

    /// <summary>
    /// <c>Token</c> é devolvido ao cliente uma única vez; só <c>TokenHash</c>
    /// é persistido (mesmo cuidado da senha — nunca guardar o segredo em
    /// texto puro). O hash usa SHA-256 simples, não Argon2id: o refresh
    /// token já nasce aleatório de alta entropia, então não precisa de uma
    /// função de derivação lenta contra força bruta — diferente da senha,
    /// que é escolhida por um humano e previsível.
    /// </summary>
    (string Token, string TokenHash, DateTimeOffset ExpiraEm) GerarRefreshToken();

    /// <summary>
    /// Calcula o mesmo hash usado em <see cref="GerarRefreshToken"/>, a partir de um
    /// token já existente — usado no refresh (RF-02) para localizar o
    /// <c>TokenRenovacao</c> correspondente pelo hash, sem guardar o token em texto puro.
    /// </summary>
    string CalcularHashDoRefreshToken(string refreshToken);
}
