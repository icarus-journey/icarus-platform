namespace Icarus.Application.Interfaces;

/// <summary>
/// Limite de tentativas de login (RF-02.3): após 5 falhas em 15 minutos para a mesma
/// combinação de e-mail normalizado + origem, novas tentativas ficam temporariamente
/// bloqueadas. A implementação concreta (armazenamento) fica em Icarus.Infrastructure.
/// </summary>
public interface ILoginAttemptLimiter
{
    bool EstaBloqueado(string email, string origem);

    void RegistrarFalha(string email, string origem);

    /// <summary>Zera o contador — um login certo não deveria continuar penalizado por falhas antigas.</summary>
    void RegistrarSucesso(string email, string origem);
}
