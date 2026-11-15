using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

public sealed class TrabalhoRelatorioSemanal : EntidadeBase
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFim { get; set; }
    public string FusoHorario { get; set; } = string.Empty;
    public string Estado { get; set; } = "processando";
    public Guid TentativaId { get; set; }
    public int Tentativas { get; set; }
    public DateTimeOffset ExpiraEm { get; set; }
    public string Indicadores { get; set; } = "{}";
    public string? FalhaCodigo { get; set; }
}
