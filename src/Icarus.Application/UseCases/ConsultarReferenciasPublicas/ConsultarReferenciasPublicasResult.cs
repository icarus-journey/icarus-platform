using Icarus.Application.Common.Analise;

namespace Icarus.Application.UseCases.ConsultarReferenciasPublicas;

public sealed record ConsultarReferenciasPublicasResult(string Estado, string? AreaDaVida, string Mensagem,
    string? VersaoGold, string? HashSnapshot, IReadOnlyList<ReferenciaSelecionada> Referencias,
    string VersaoContrato = "1.2.0", string? VersaoCatalogo = null,
    IReadOnlyList<EvidenciaPesquisa>? Evidencias = null, MetaPesquisa? Meta = null,
    Icarus.Application.UseCases.RelatoSono.RelatoSonoResult? RelatoSono = null, ContextoSono? ContextoSono = null,
    IReadOnlyList<LeituraQuantitativa>? Leituras = null);
