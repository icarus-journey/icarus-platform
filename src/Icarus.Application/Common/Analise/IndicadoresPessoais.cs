namespace Icarus.Application.Common.Analise;

public sealed record ConclusoesPorArea(string AreaDaVida, int Concluidas);
public sealed record IndicadoresPessoais(int? Previstas, int? Concluidas, decimal? TaxaConclusao,
    int? Pendentes, int? Atrasadas, int? ConcluidasComAtraso, int? Sequencia,
    long? PontosGanhos, long? PontosUtilizados, int? RecompensasResgatadas, int? RecompensasUtilizadas,
    IReadOnlyList<ConclusoesPorArea> DistribuicaoPorArea, int? ConclusoesSemArea, int? MaiorAtividadeDeCampanha,
    decimal? ProgressoManualCampanhas, int DiasComConclusao, int QuantidadeRegistros,
    bool HistoricoInsuficienteComparacao, IReadOnlyList<string> Limitacoes);
