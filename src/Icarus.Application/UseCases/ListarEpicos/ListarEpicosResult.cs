using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ListarEpicos;

public sealed record EpicoListadoResult(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    EstadoEpico Estado,
    List<CampanhaListadaResult> Campanhas);

public sealed record CampanhaListadaResult(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    decimal? ValorMensurado,
    string? Unidade,
    decimal? ValorAtual,
    EstadoCampanha Estado,
    List<MissaoListadaResult> Missoes);

public sealed record MissaoListadaResult(
    long Id,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly? DataLimite,
    DateTimeOffset Horario,
    StatusMissao Status);
