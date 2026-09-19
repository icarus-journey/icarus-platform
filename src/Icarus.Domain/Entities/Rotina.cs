using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

/// <summary>
/// Contexto de disponibilidade/ocupação do usuário em um período de vigência.
/// Um usuário pode ter várias rotinas ao longo do tempo (histórico).
/// </summary>
public class Rotina : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public int HorasLivres { get; set; }
    public int HorasOcupado { get; set; }
    public string AreaTrabalho { get; set; } = string.Empty;
    public string ModalidadeTrabalho { get; set; } = string.Empty;
    public DateOnly VigenciaInicio { get; set; }

    /// <summary>Nulo enquanto a rotina for a vigente.</summary>
    public DateOnly? VigenciaFim { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
