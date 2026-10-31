namespace Icarus.Application.Common.Jornada;

public sealed record RealizacaoSequencia(DateTimeOffset? RealizadaEm, string? FusoHorario,
    DateTimeOffset? RegistradaEm);

public sealed record DadosJornada(long SaldoPontos, string? FusoHorario,
    IReadOnlyList<RealizacaoSequencia> Realizacoes);

public sealed record DiaSequencia(DateOnly Data, bool? Realizada);

public sealed record SequenciaDiaria(string Estado, int? DiasAtuais, bool? RealizouHoje,
    DateOnly? Hoje, string? FusoHorario, IReadOnlyList<DiaSequencia> Dias);
