namespace Icarus.Domain.Enums;

/// <summary>
/// Ciclo de vida do épico (DER v1.1, docs/implementation-design/platform/02-modelos-de-dados-postgresql.md,
/// seção 7).
/// </summary>
public enum EstadoEpico
{
    EmAndamento,
    Concluido,
    Excluido
}
