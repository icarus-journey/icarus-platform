using System.Globalization;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public sealed record MedicaoCampanhaResult(long Id, decimal? ValorAnterior, decimal ValorAtual,
    decimal ValorMensurado, string Unidade, DateTimeOffset RegistradaEm, string? FusoHorario);

public sealed record ProgressoCampanhaResult(string VersaoContrato, long CampanhaId, decimal? ValorAtual,
    decimal? ValorMensurado, string? Unidade, EstadoCampanha Estado, bool PodeRegistrar,
    bool MetaAlcancada, string Versao, IReadOnlyList<MedicaoCampanhaResult> Medicoes)
{
    // PostgreSQL preserva microssegundos; a versão devolvida deve ter a mesma precisão.
    public static string VersaoDaCampanha(Campanha c) => c.AtualizadoEm.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);

    public static ProgressoCampanhaResult DaCampanha(Campanha c, IReadOnlyList<MedicaoCampanha> medicoes) => new(
        "1.0.0", c.Id, c.ValorAtual, c.ValorMensurado, c.Unidade, c.Estado,
        (c.Estado is EstadoCampanha.Planejada or EstadoCampanha.EmAndamento) && c.ValorMensurado > 0 && !string.IsNullOrWhiteSpace(c.Unidade),
        c.ValorMensurado > 0 && c.ValorAtual >= c.ValorMensurado, VersaoDaCampanha(c),
        medicoes.Select(m => new MedicaoCampanhaResult(m.Id, m.ValorAnterior, m.ValorAtual, m.ValorMensurado, m.Unidade, m.RegistradaEm, m.FusoHorario)).ToArray());
}
