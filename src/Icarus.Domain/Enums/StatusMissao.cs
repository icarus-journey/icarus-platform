using Icarus.Domain.Entities;

namespace Icarus.Domain.Enums;

/// <summary>
/// Ciclo de vida da missão (DER v1.2, docs/implementation-design/platform/02-modelos-de-dados-postgresql.md,
/// seção 10; RF-04 em 04-requisitos.md). Não confundir com o estado de
/// execução, que pertence a cada <see cref="Ocorrencia"/> — ver
/// <see cref="StatusOcorrencia"/>.
/// </summary>
public enum StatusMissao
{
    Rascunho,
    Ativa,
    Inativa
}
