using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

public sealed class InboxRelatorio : EntidadeBase
{
    public Guid Id { get; set; }
    public Guid TrabalhoId { get; set; }
    public Guid TentativaId { get; set; }
    public string Estado { get; set; } = string.Empty;
}
