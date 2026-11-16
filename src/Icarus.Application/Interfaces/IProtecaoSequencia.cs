using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface IProtecaoSequencia
{
    // Executado sob o bloqueio do usuário, dentro da transação da operação chamadora.
    Task ReconciliarAsync(Usuario usuario, CancellationToken ct);
    void SolicitarRecalculo(Guid usuarioId);
}
