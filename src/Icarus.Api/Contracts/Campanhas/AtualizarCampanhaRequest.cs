using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Campanhas;

/// <summary>Sem AreaDaVida (herdada, imutável) nem EpicoId (sem re-vínculo via edição).</summary>
public sealed record AtualizarCampanhaRequest(
    string Titulo,
    string? Descricao,
    Dificuldade Dificuldade,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    decimal? ValorMensurado,
    string? Unidade);
