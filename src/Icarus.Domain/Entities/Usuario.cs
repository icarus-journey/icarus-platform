using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

public class Usuario : EntidadeBase
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly DataNascimento { get; set; }

    /// <summary>
    /// Saldo atual, mantido em sincronia com <see cref="MovimentacaoPontos"/> na mesma transação.
    /// </summary>
    public long SaldoPontos { get; set; }

    public ICollection<Rotina> Rotinas { get; set; } = new List<Rotina>();
    public ICollection<Epico> Epicos { get; set; } = new List<Epico>();
    public ICollection<Missao> Missoes { get; set; } = new List<Missao>();
    public ICollection<MissaoIA> MissoesIA { get; set; } = new List<MissaoIA>();
    public ICollection<Diario> Diarios { get; set; } = new List<Diario>();
    public ICollection<Relatorio> Relatorios { get; set; } = new List<Relatorio>();
    public ICollection<Item> Itens { get; set; } = new List<Item>();
    public ICollection<InventarioItem> InventarioItens { get; set; } = new List<InventarioItem>();
    public ICollection<MovimentacaoPontos> MovimentacoesPontos { get; set; } = new List<MovimentacaoPontos>();
}
