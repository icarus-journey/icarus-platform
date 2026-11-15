using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Epico"/>. A implementação concreta (EF Core)
/// fica em Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IEpicoRepository
{
    /// <summary>
    /// Adiciona o épico e, por grafo de navegação do EF, suas campanhas e missões já
    /// atribuídas em memória (usado ao persistir o resultado do onboarding de uma só vez).
    /// </summary>
    Task AdicionarAsync(Epico epico, CancellationToken cancellationToken);

    /// <summary>Carrega o épico com sua campanha e missões, restrito ao usuário dono (RF-02.4).</summary>
    Task<Epico?> BuscarComCampanhaEMissoesAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Carrega só o épico (sem grafo), restrito ao usuário dono — usado para edição e
    /// para validar a posse do épico referenciado ao criar uma campanha.</summary>
    Task<Epico?> BuscarPorIdAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Lista todos os épicos do usuário, com campanhas e missões (RF-01.5).</summary>
    Task<List<Epico>> ListarComCampanhasEMissoesAsync(Guid usuarioId, CancellationToken cancellationToken);
}
