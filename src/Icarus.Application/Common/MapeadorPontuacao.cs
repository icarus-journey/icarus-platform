using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

/// <summary>
/// Único lugar do sistema que decide quantos pontos uma dificuldade vale. Épico, Campanha e
/// Missão nunca recebem pontuação vinda do front ou da IA — ela é sempre calculada aqui a
/// partir da dificuldade escolhida.
/// </summary>
public static class MapeadorPontuacao
{
    private static readonly Dictionary<Dificuldade, int> PontuacaoPorDificuldade = new()
    {
        [Dificuldade.Trivial] = 15,
        [Dificuldade.Facil] = 30,
        [Dificuldade.Media] = 45,
        [Dificuldade.Dificil] = 60
    };

    public static int ParaPontuacao(Dificuldade dificuldade) => PontuacaoPorDificuldade[dificuldade];
}
