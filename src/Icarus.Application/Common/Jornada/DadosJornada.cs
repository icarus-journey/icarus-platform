namespace Icarus.Application.Common.Jornada;

public sealed record RealizacaoSequencia(DateTimeOffset? RealizadaEm, string? FusoHorario,
    DateTimeOffset? RegistradaEm);

public sealed record DadosJornada(long SaldoPontos, string? FusoHorario,
    IReadOnlyList<RealizacaoSequencia> Realizacoes, IReadOnlyList<DateOnly>? DiasProtegidos = null, int ProtecoesPendentes = 0);

public sealed record DiaSequencia(DateOnly Data, bool? Realizada, bool Protegida = false);

public sealed record SequenciaDiaria(string Estado, int? DiasAtuais, bool? RealizouHoje,
    DateOnly? Hoje, string? FusoHorario, IReadOnlyList<DiaSequencia> Dias);
