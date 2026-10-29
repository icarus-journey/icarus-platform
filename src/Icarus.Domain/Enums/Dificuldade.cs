namespace Icarus.Domain.Enums;

/// <summary>
/// Dificuldade de Épico, Campanha ou Missão. Determina a pontuação concedida ao concluir
/// (ver Icarus.Application.Common.MapeadorPontuacao) — nunca é informada diretamente como
/// pontuação pelo front ou pela IA.
/// </summary>
public enum Dificuldade
{
    Trivial,
    Facil,
    Media,
    Dificil
}
