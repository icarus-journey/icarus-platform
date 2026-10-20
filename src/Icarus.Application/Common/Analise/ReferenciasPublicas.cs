namespace Icarus.Application.Common.Analise;

public sealed record FiltrosReferencia(bool? Estuda = null, bool? Trabalha = null);
public sealed record OrigemPublica(string Fonte, string Edicao, string UrlFonte, string Sha256);
public sealed record MetadadosPublicos(string VersaoGold, string VersaoSilver, string VersaoContrato,
    string VersaoCatalogo, string VersaoPolitica, IReadOnlyList<OrigemPublica> Fontes);

public sealed class ValoresReferencia
{
    public string Unidade { get; init; } = string.Empty;
    public decimal? Percentual { get; init; }
    public decimal? P25 { get; init; }
    public decimal? Mediana { get; init; }
    public decimal? P75 { get; init; }
    public decimal? Madrugada { get; init; }
    public decimal? Manha { get; init; }
    public decimal? Tarde { get; init; }
    public decimal? Noite { get; init; }
    public decimal? GrupoExpostoPercentual { get; init; }
    public decimal? GrupoNaoExpostoPercentual { get; init; }
    public decimal? DiferencaPp { get; init; }
    public string? Exposicao { get; init; }
    public string? Desfecho { get; init; }
    public string? Interpretacao { get; init; }
}

public sealed class ReferenciaPublica
{
    public string Id { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Indicador { get; init; } = string.Empty;
    public string Medida { get; init; } = string.Empty;
    public string Fonte { get; init; } = string.Empty;
    public int Edicao { get; init; }
    public string AreaDaVida { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Pergunta { get; init; } = string.Empty;
    public string Reflexao { get; init; } = string.Empty;
    public string Populacao { get; init; } = string.Empty;
    public string Denominador { get; init; } = string.Empty;
    public string FaixaEtaria { get; init; } = string.Empty;
    public string TipoDia { get; init; } = string.Empty;
    public FiltrosReferencia Filtros { get; init; } = new();
    public ValoresReferencia Valores { get; init; } = new();
    public IReadOnlyList<string> Limitacoes { get; init; } = [];
}

public sealed record SnapshotReferenciasPublicas(string Hash, MetadadosPublicos Metadados,
    IReadOnlyList<ReferenciaPublica> Referencias);
public sealed record RecorteReferencia(string FaixaSolicitada, string FaixaEfetiva, string TipoDiaSolicitado,
    string TipoDiaEfetivo, FiltrosReferencia FiltrosEfetivos, bool Ampliado, IReadOnlyList<string> Avisos);
public sealed record ReferenciaSelecionada(ReferenciaPublica Dados, RecorteReferencia Recorte,
    string Pais, IReadOnlyList<OrigemPublica> Origens);
