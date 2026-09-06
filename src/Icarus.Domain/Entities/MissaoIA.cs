using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>
/// Registro de auditoria de uma missão sugerida pela IA. Possui os mesmos campos de
/// <see cref="Missao"/>, mais o resultado da revisão humana da sugestão.
/// </summary>
/// <remarks>
/// <see cref="MissaoId"/> só é preenchido quando a sugestão é aprovada e a missão real
/// é criada; até lá permanece nulo. <see cref="CampanhaId"/> segue a mesma regra de
/// <see cref="Missao.CampanhaId"/>: nulo representa sugestão avulsa.
/// </remarks>
public class MissaoIA : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public long? CampanhaId { get; set; }
    public long? MissaoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Pontuacao { get; set; }
    public string? Beneficio { get; set; }
    public DateOnly? DataLimite { get; set; }
    public DateTimeOffset Horario { get; set; }
    public StatusMissao Status { get; set; } = StatusMissao.Pendente;
    public StatusAprovacaoMissaoIA StatusAprovacao { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Campanha? Campanha { get; set; }
    public Missao? Missao { get; set; }
}
