using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

/// <summary>
/// CampanhaId nulo cria uma missão avulsa (RF-03). Horario no formato "HH:mm"; omitido assume
/// 09:00. Recorrencia nula cria missão não recorrente (uma única ocorrência); preenchida gera
/// a janela dos próximos 30 dias (RF-04.4/RF-04.5). DiasSemana só é usado (e é obrigatório, não
/// vazio) quando Recorrencia é "semanal"; para "mensal" o dia do mês vem de DataLimite (ou
/// hoje, se nula). DataFimRecorrencia é opcional e só se aplica quando Recorrencia é
/// preenchida: nula repete indefinidamente (até parar manualmente — RF-04.6); preenchida para
/// de gerar novas ocorrências a partir dessa data (a missão continua ativa).
/// </summary>
public sealed record CriarMissaoRequest(
    long? CampanhaId,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    DateOnly? DataLimite,
    string? Horario,
    TipoRecorrencia? Recorrencia,
    List<DayOfWeek>? DiasSemana,
    DateOnly? DataFimRecorrencia);
