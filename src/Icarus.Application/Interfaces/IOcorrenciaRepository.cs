using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Ocorrencia"/>. A implementação concreta (EF Core) fica em
/// Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IOcorrenciaRepository
{
    Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken);

    /// <summary>
    /// Busca a ocorrência de uma missão numa data prevista (missao_id + data_prevista é único —
    /// DER seção 12). Para missão não recorrente existe no máximo uma linha.
    /// </summary>
    Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long missaoId, DateOnly dataPrevista, CancellationToken cancellationToken);

    /// <summary>
    /// Resolve qual ocorrência concluir agora (RF-04.6): a de hoje (se existir, qualquer
    /// status — cobre tanto "concluir hoje" quanto a idempotência de concluir de novo no mesmo
    /// dia); senão a pendente/atrasada mais antiga (recupera um dia perdido); senão, pra missão
    /// não recorrente cuja única ocorrência não é hoje, a mais recente já concluída
    /// (idempotência); senão nulo (nenhuma ocorrência ainda — o chamador decide o que fazer).
    /// </summary>
    Task<Ocorrencia?> BuscarParaConcluirAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken);

    /// <summary>Datas (dentro do intervalo) que já têm ocorrência gerada para a missão — evita duplicar ao estender a janela.</summary>
    Task<List<DateOnly>> ListarDatasExistentesAsync(long missaoId, DateOnly de, DateOnly ate, CancellationToken cancellationToken);

    /// <summary>
    /// Ocorrências pendentes cuja data prevista ainda não passou (RF-04.6) — as que a
    /// inativação cancela. As pendentes já vencidas ficam de fora: são preservadas como
    /// histórico (atrasadas), sem permitir conclusão.
    /// </summary>
    Task<List<Ocorrencia>> ListarPendentesNaoVencidasAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken);
}
