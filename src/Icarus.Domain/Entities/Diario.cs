using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

/// <summary>Entrada de diário do usuário. CRUD isolado; nenhum consumidor analítico.</summary>
public class Diario : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }

    /// <summary>Dia ao qual a entrada pertence (pode ser anterior a <see cref="EntidadeBase.CriadoEm"/>).</summary>
    public DateOnly DataRegistro { get; set; }

    public string Texto { get; set; } = string.Empty;

    public Usuario Usuario { get; set; } = null!;
}
