using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Campanhas;

public sealed record MedicaoCampanhaResponse(long Id, decimal? ValorAnterior, decimal ValorAtual,
    decimal ValorMensurado, string Unidade, DateTimeOffset RegistradaEm, string? FusoHorario);

public sealed record ProgressoCampanhaResponse(string VersaoContrato, long CampanhaId, decimal? ValorAtual,
    decimal? ValorMensurado, string? Unidade, EstadoCampanha Estado, bool PodeRegistrar,
    bool MetaAlcancada, string Versao, IReadOnlyList<MedicaoCampanhaResponse> Medicoes)
{
    public static ProgressoCampanhaResponse DoResultado(ProgressoCampanhaResult r) => new(
        r.VersaoContrato, r.CampanhaId, r.ValorAtual, r.ValorMensurado, r.Unidade, r.Estado,
        r.PodeRegistrar, r.MetaAlcancada, r.Versao,
        r.Medicoes.Select(m => new MedicaoCampanhaResponse(m.Id, m.ValorAnterior, m.ValorAtual,
            m.ValorMensurado, m.Unidade, m.RegistradaEm, m.FusoHorario)).ToArray());
}
