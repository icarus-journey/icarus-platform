using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>Objetivo de longo prazo do usuário. Contém várias campanhas.</summary>
public class Epico : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string AreaDaVida { get; set; } = string.Empty;
    public int Priorizacao { get; set; }
    public string Cor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateOnly PrazoInicio { get; set; }
    public DateOnly PrazoFim { get; set; }
    public EstadoEpico Estado { get; set; } = EstadoEpico.EmAndamento;

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Campanha> Campanhas { get; set; } = new List<Campanha>();
}
