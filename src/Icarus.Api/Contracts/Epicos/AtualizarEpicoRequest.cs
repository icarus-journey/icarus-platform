using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Epicos;

/// <summary>Sem AreaDaVida: imutável após criado (RF-03.2).</summary>
public sealed record AtualizarEpicoRequest(
    string Titulo,
    string? Descricao,
    Dificuldade Dificuldade,
    DateOnly PrazoInicio,
    DateOnly PrazoFim);
