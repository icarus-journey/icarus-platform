namespace Icarus.Domain.Entities;

/// <summary>Decisão vigente por dia civil; os consumos anteriores permanecem no histórico.</summary>
public sealed class FechamentoJornada
{
    public Guid UsuarioId { get; set; }
    public DateOnly Data { get; set; }
    public string FusoHorario { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTimeOffset AtualizadoEm { get; set; }
}
