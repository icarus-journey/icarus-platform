using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>Item consumível disponível na loja do usuário.</summary>
public class Item : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>Custo em pontos necessário para aquisição.</summary>
    public int Valor { get; set; }

    public string? Descricao { get; set; }

    /// <summary>Indica se o item controla o aplicativo (ex.: proteção de sequência).</summary>
    public ControlaApp ControlaApp { get; set; } = ControlaApp.Nao;

    public Usuario Usuario { get; set; } = null!;
    public ICollection<InventarioItem> InventarioItens { get; set; } = new List<InventarioItem>();
    public ICollection<MovimentacaoPontos> MovimentacoesPontos { get; set; } = new List<MovimentacaoPontos>();
}
