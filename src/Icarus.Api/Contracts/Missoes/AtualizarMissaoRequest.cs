using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

/// <summary>Sem CampanhaId (sem re-vínculo via edição) nem Status (ciclo de vida fora de escopo aqui).</summary>
public sealed record AtualizarMissaoRequest(
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    DateOnly? DataLimite,
    string? Horario,
    TipoRecorrencia? Recorrencia = null,
    List<DayOfWeek>? DiasSemana = null,
    DateOnly? DataFimRecorrencia = null,
    int? DiaDoMes = null,
    string? VersaoEsperada = null);
