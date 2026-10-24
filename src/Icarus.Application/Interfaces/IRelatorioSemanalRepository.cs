using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface IRelatorioSemanalRepository
{
    Task ExecutarAsync(Guid usuarioId, Func<Usuario, CancellationToken, Task> operacao, CancellationToken ct);
    Task<TrabalhoRelatorioSemanal?> BuscarTrabalhoAsync(Guid usuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct);
    Task<TrabalhoRelatorioSemanal?> BuscarPorIdAsync(Guid id, CancellationToken ct);
    Task<Guid?> BuscarTitularAsync(Guid id, CancellationToken ct);
    Task<Relatorio?> BuscarRelatorioAsync(Guid usuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct);
    Task<bool> ResultadoRecebidoAsync(Guid idMensagem, CancellationToken ct);
    Task AdicionarTrabalhoAsync(TrabalhoRelatorioSemanal trabalho, CancellationToken ct);
    Task AdicionarOutboxAsync(OutboxRelatorio mensagem, CancellationToken ct);
    Task AdicionarInboxAsync(InboxRelatorio mensagem, CancellationToken ct);
    Task AdicionarRelatorioAsync(Relatorio relatorio, CancellationToken ct);
}
