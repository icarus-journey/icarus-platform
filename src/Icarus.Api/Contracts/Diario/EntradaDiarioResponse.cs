using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Diario;

public sealed record EntradaDiarioResponse(long Id, TipoEntradaDiario Tipo, DateOnly DataRegistro,
    string Texto, DateTimeOffset CriadaEm, DateTimeOffset AtualizadaEm)
{
    public static EntradaDiarioResponse DoResultado(EntradaDiarioResult resultado) => new(resultado.Id,
        resultado.Tipo, resultado.DataRegistro, resultado.Texto, resultado.CriadaEm, resultado.AtualizadaEm);
}
