using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public sealed record EntradaDiarioResult(long Id, TipoEntradaDiario Tipo, DateOnly DataRegistro,
    string Texto, DateTimeOffset CriadaEm, DateTimeOffset AtualizadaEm)
{
    public static EntradaDiarioResult DaEntrada(Diario entrada) => new(entrada.Id, entrada.Tipo,
        entrada.DataRegistro, entrada.Texto, entrada.CriadoEm, entrada.AtualizadoEm);
}
