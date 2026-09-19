using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>Etapa concreta e de menor duração dentro de um épico. Contém várias missões.</summary>
public class Campanha : EntidadeBase
{
    public long Id { get; set; }
    public long EpicoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int Priorizacao { get; set; }
    public string Cor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateOnly PrazoInicio { get; set; }
    public DateOnly PrazoFim { get; set; }

    /// <summary>Valor-alvo mensurado da campanha. Nulo quando a campanha não é mensurável.</summary>
    public decimal? ValorMensurado { get; set; }

    public string? Unidade { get; set; }
    public decimal? ValorAtual { get; set; }
    public EstadoCampanha Estado { get; set; } = EstadoCampanha.Planejada;

    public Epico Epico { get; set; } = null!;
    public ICollection<Missao> Missoes { get; set; } = new List<Missao>();
    public ICollection<MissaoIA> MissoesIA { get; set; } = new List<MissaoIA>();
}
