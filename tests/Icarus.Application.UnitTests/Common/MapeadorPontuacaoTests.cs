using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Application.UnitTests.Common;

public class MapeadorPontuacaoTests
{
    [Theory(DisplayName = "Cada dificuldade mapeia pra pontuação fixa (15/30/45/60)")]
    [InlineData(Dificuldade.Trivial, 15)]
    [InlineData(Dificuldade.Facil, 30)]
    [InlineData(Dificuldade.Media, 45)]
    [InlineData(Dificuldade.Dificil, 60)]
    public void ParaPontuacao_DeveDevolverValorFixoPorDificuldade(Dificuldade dificuldade, int pontuacaoEsperada)
    {
        Assert.Equal(pontuacaoEsperada, MapeadorPontuacao.ParaPontuacao(dificuldade));
    }
}
