namespace Icarus.Domain.Enums;

/// <summary>
/// Ciclo de vida da campanha (DER v1.1, docs/implementation-design/platform/02-modelos-de-dados-postgresql.md,
/// seção 8).
/// </summary>
public enum EstadoCampanha
{
    Planejada,
    EmAndamento,
    Concluida,
    Excluida
}
