using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

/// <summary>
/// Regra de repetição de uma missão (no máximo uma por missão).
/// A ocorrência concreta é calculada pela aplicação a partir de <see cref="Regra"/>.
/// </summary>
public class Recorrencia : EntidadeBase
{
    public long Id { get; set; }
    public long MissaoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }

    /// <summary>Configuração da recorrência (ex.: dias da semana, hora). Armazenado como JSONB.</summary>
    public string Regra { get; set; } = "{}";

    public Missao Missao { get; set; } = null!;
}
