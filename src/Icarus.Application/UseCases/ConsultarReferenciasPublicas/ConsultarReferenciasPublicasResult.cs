using Icarus.Application.Common.Analise;

namespace Icarus.Application.UseCases.ConsultarReferenciasPublicas;

public sealed record ConsultarReferenciasPublicasResult(string Estado, string? AreaDaVida, string Mensagem,
    string? VersaoGold, string? HashSnapshot, IReadOnlyList<ReferenciaSelecionada> Referencias,
    string VersaoContrato = "1.0.0", string? VersaoCatalogo = null);
