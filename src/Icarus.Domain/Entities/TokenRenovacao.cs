using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

/// <summary>
/// Refresh token: string aleatória de longa duração usada para obter um novo
/// access token (JWT) sem exigir login novamente. Guardamos só o hash do
/// token, nunca o valor em texto puro — o mesmo cuidado aplicado à senha.
/// </summary>
/// <remarks>
/// Divergência deliberada do RF-02 do DER (que previa JWT de 24h sem
/// renovação): adotamos o padrão access token curto + refresh token de longa
/// duração, com rotação — cada uso de um refresh token gera um par novo e
/// marca o antigo como substituído, permitindo detectar reuso indevido
/// (alguém usando um refresh token que já foi trocado por outro). Ver
/// PROJETO.md.
/// </remarks>
public class TokenRenovacao : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? RevogadoEm { get; set; }

    /// <summary>Token novo que substituiu este na rotação (nulo enquanto este for o vigente).</summary>
    public long? SubstituidoPorId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public TokenRenovacao? SubstituidoPor { get; set; }
}
