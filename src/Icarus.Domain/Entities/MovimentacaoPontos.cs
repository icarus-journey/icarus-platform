using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>
/// Histórico imutável do saldo de pontos do usuário. Nunca é atualizada após
/// criada — por isso não herda de <see cref="EntidadeBase"/> (sem AtualizadoEm).
/// </summary>
public class MovimentacaoPontos
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public long? ItemId { get; set; }

    /// <summary>Origem de negócio de um crédito por ocorrência concluída.</summary>
    public long? OcorrenciaId { get; set; }

    public TipoMovimentacaoPontos Tipo { get; set; }

    /// <summary>Positivo para crédito, negativo para débito.</summary>
    public long Quantidade { get; set; }

    public string? Descricao { get; set; }
    public DateTimeOffset CriadoEm { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Item? Item { get; set; }
    public Ocorrencia? Ocorrencia { get; set; }
}
