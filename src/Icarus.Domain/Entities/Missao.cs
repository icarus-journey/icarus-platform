using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>
/// Representa tanto uma missão de campanha (<see cref="CampanhaId"/> preenchido)
/// quanto uma missão avulsa (<see cref="CampanhaId"/> nulo).
/// </summary>
public class Missao : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public long? CampanhaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Pontuacao { get; set; }
    public string? Beneficio { get; set; }
    public DateOnly? DataLimite { get; set; }
    public DateTimeOffset Horario { get; set; }
    public StatusMissao Status { get; set; } = StatusMissao.Pendente;
    public DateTimeOffset? ConcluidaEm { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Campanha? Campanha { get; set; }
    public Recorrencia? Recorrencia { get; set; }
    public ICollection<Ocorrencia> Ocorrencias { get; set; } = new List<Ocorrencia>();
    public ICollection<MissaoIA> MissoesIA { get; set; } = new List<MissaoIA>();
}
