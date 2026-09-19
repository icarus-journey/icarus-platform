namespace Icarus.Domain.Enums;

/// <summary>
/// Indica se o item controla o aplicativo (ex.: proteção de sequência) — DER v1.1,
/// docs/implementation-design/platform/02-modelos-de-dados-postgresql.md, seção 15.
/// Modelado como enum de texto (não bool) para seguir o vocabulário SIM/NAO do DER.
/// </summary>
public enum ControlaApp
{
    Nao,
    Sim
}
