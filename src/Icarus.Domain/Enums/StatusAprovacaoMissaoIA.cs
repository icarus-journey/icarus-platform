namespace Icarus.Domain.Enums;

/// <summary>
/// Resultado da revisão humana de uma sugestão de missão gerada pela IA (DER v1.1,
/// docs/implementation-design/platform/02-modelos-de-dados-postgresql.md, seção 9).
/// </summary>
public enum StatusAprovacaoMissaoIA
{
    Aprovado,
    Recusado
}
