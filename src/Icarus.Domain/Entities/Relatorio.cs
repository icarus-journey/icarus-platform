using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>
/// Snapshot histórico do estado do usuário em um período. Não é recalculado
/// nem alterado depois de persistido; não possui FK direta para outras entidades.
/// </summary>
public class Relatorio : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public TipoRelatorio Tipo { get; set; }
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFim { get; set; }

    /// <summary>Resultado consolidado (ex.: pontos, missões concluídas). Armazenado como JSONB.</summary>
    public string Conteudo { get; set; } = "{}";

    public string? Texto { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
