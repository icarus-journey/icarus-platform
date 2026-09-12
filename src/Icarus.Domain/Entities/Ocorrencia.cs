using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>
/// Execução planejada de uma missão, única por missão e data prevista.
/// </summary>
public class Ocorrencia : EntidadeBase
{
    public long Id { get; set; }
    public long MissaoId { get; set; }
    public DateOnly DataPrevista { get; set; }
    public StatusOcorrencia Status { get; set; } = StatusOcorrencia.Pendente;
    public DateTimeOffset? RealizadaEm { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }

    public Missao Missao { get; set; } = null!;
    public ICollection<MovimentacaoPontos> MovimentacoesPontos { get; set; } = new List<MovimentacaoPontos>();
}
