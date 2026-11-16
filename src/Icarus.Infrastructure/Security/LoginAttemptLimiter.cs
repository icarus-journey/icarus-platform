using Icarus.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Icarus.Infrastructure.Security;

/// <summary>
/// Implementação em memória (<see cref="IMemoryCache"/>) do limite de tentativas de
/// login (RF-02.3). Aceito como limitação conhecida do MVP local: o contador se perde
/// se a API reiniciar (documentado em 01-arquitetura-da-solucao.md, seção 17, no
/// icarus-context) — persistir ou externalizar só seria necessário numa publicação real.
/// </summary>
public sealed class LoginAttemptLimiter(IMemoryCache cache) : ILoginAttemptLimiter
{
    private const int MaximoDeFalhas = 5;
    private static readonly TimeSpan JanelaDeTempo = TimeSpan.FromMinutes(15);

    public bool EstaBloqueado(string email, string origem)
        => cache.TryGetValue<ContadorDeFalhas>(ChaveDeCache(email, origem), out var contador)
            && contador!.Quantidade >= MaximoDeFalhas;

    public void RegistrarFalha(string email, string origem)
    {
        var chave = ChaveDeCache(email, origem);
        var existente = cache.TryGetValue<ContadorDeFalhas>(chave, out var contadorAtual) ? contadorAtual : null;

        var novoContador = existente is null
            ? new ContadorDeFalhas(1, DateTimeOffset.UtcNow)
            : existente with { Quantidade = existente.Quantidade + 1 };

        // Expira exatamente no fim da janela dos 15 min contados a partir da primeira
        // falha — depois disso, a próxima falha começa uma janela nova.
        cache.Set(chave, novoContador, novoContador.PrimeiraFalhaEm.Add(JanelaDeTempo));
    }

    public void RegistrarSucesso(string email, string origem)
        => cache.Remove(ChaveDeCache(email, origem));

    private static string ChaveDeCache(string email, string origem) => $"login-falhas:{email}:{origem}";

    private sealed record ContadorDeFalhas(int Quantidade, DateTimeOffset PrimeiraFalhaEm);
}
