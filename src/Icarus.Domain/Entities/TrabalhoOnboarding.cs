using Icarus.Domain.Common;
using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>
/// Rastreia um job de geração de onboarding por IA enviado ao RabbitMQ. O <see cref="Id"/>
/// é o próprio job_id publicado na fila — serve de correlação com o resultado assíncrono e
/// de chave de idempotência (um mesmo job_id nunca é processado duas vezes).
/// </summary>
public class TrabalhoOnboarding : EntidadeBase
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public StatusTrabalhoOnboarding Status { get; set; } = StatusTrabalhoOnboarding.Pendente;

    /// <summary>Preenchido quando <see cref="Status"/> é <see cref="StatusTrabalhoOnboarding.Concluido"/>.</summary>
    public long? EpicoId { get; set; }

    /// <summary>Preenchido quando <see cref="Status"/> é <see cref="StatusTrabalhoOnboarding.Falhou"/>.</summary>
    public string? ErroMensagem { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Epico? Epico { get; set; }
}
