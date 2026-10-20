using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface ILojaRepository
{
    /// <summary>Bloqueia o saldo antes de ler itens; confirma tudo ou reverte tudo.</summary>
    Task ExecutarAsync(Guid usuarioId, Func<Usuario, CancellationToken, Task> operacao, CancellationToken cancelamento);
    Task<List<Item>> ListarItensAsync(Guid usuarioId, CancellationToken cancelamento);
    Task<List<ResgateItem>> ListarResgatesAsync(Guid usuarioId, CancellationToken cancelamento);
    Task AdicionarItemAsync(Item item, CancellationToken cancelamento);
    Task AdicionarResgateAsync(ResgateItem resgate, CancellationToken cancelamento);
    Task AdicionarMovimentacaoAsync(MovimentacaoPontos movimentacao, CancellationToken cancelamento);
}
