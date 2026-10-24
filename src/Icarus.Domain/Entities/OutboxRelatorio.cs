using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

public sealed class OutboxRelatorio : EntidadeBase
{
    public Guid Id { get; set; }
    public Guid TrabalhoId { get; set; }
    public string? Carga { get; set; }
    public int Tentativas { get; set; }
    public DateTimeOffset ProximaTentativaEm { get; set; }
    public DateTimeOffset? PublicadaEm { get; set; }
}
