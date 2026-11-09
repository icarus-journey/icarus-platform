using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common.Analise;

public sealed record AtividadeObjetivo(long CampanhaId, long EpicoId, DateTimeOffset? RealizadaEm, DateTimeOffset? RegistradaEm);
public sealed record AtividadeNoObjetivo(int? ConcluidasNoPeriodo, int RegistrosConfirmadosNoPeriodo,
    int DiasComRegistrosNoPeriodo, int? ConcluidasNosUltimosSeteDias, DateTimeOffset? UltimaRealizacao);
public sealed record ResultadoNoObjetivo(string Estado, decimal? ValorAtual, decimal? ValorMensurado,
    string? Unidade, int RegistrosNoPeriodo, int DiasComRegistrosNoPeriodo, string? BaseDoAvanco,
    EvolucaoMeta EvolucaoNoPeriodo, CenarioConquista CenarioAtual, EvolucaoMeta? EvolucaoAtual = null);
public sealed record CampanhaNoPanorama(long Id, long EpicoId, string Titulo, AreaDaVida AreaDaVida,
    EstadoCampanha Estado, DateOnly PrazoFim, AtividadeNoObjetivo Atividade, ResultadoNoObjetivo Resultado);
public sealed record CampanhasPorEstado(int Planejadas, int EmAndamento, int Concluidas);
public sealed record EpicoNoPanorama(long Id, string Titulo, EstadoEpico Estado,
    CampanhasPorEstado Campanhas, AtividadeNoObjetivo Atividade);
public sealed record PanoramaObjetivos(DateTimeOffset ConsultadoEm, PeriodoAnalitico JanelaRecente,
    int CampanhasEmAndamento, int EpicosEmAndamento, int? CampanhasSemRegistroRecente,
    string EstadoDestaque, long? CampanhaEmDestaqueId,
    IReadOnlyList<CampanhaNoPanorama> Campanhas, IReadOnlyList<EpicoNoPanorama> Epicos);
