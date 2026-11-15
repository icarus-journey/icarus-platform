namespace Icarus.Application.Interfaces;

public interface ITransacaoConclusaoMissao
{
    /// <summary>Recarrega o estado sob bloqueio do saldo e confirma ou reverte toda a conclusão.</summary>
    Task<T> ExecutarAsync<T>(Guid usuarioId, Func<CancellationToken, Task<T>> operacao, CancellationToken ct);
}
