namespace Icarus.Domain.Enums;

/// <summary>
/// Estado explícito da missão (DER v1.1, docs/implementation-design/platform/02-modelos-de-dados-postgresql.md,
/// seção 10). É uma decisão de domínio em aberto e pode evoluir.
/// </summary>
public enum StatusMissao
{
    Pendente,
    Concluida,
    Cancelada,
    ConcluidaComAtraso
}
