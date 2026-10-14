using Icarus.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;

namespace Icarus.Infrastructure.UnitTests.Security;

public class LoginAttemptLimiterTests
{
    private const string Email = "ana@exemplo.com";
    private const string Origem = "127.0.0.1";

    private readonly LoginAttemptLimiter _limiter = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact(DisplayName = "EstaBloqueado retorna falso quando não há tentativas registradas")]
    public void EstaBloqueado_DeveRetornarFalsoSemTentativas()
        => Assert.False(_limiter.EstaBloqueado(Email, Origem));

    [Fact(DisplayName = "EstaBloqueado retorna falso com menos de 5 falhas")]
    public void EstaBloqueado_DeveRetornarFalsoComMenosDeCincoFalhas()
    {
        for (var i = 0; i < 4; i++)
        {
            _limiter.RegistrarFalha(Email, Origem);
        }

        Assert.False(_limiter.EstaBloqueado(Email, Origem));
    }

    [Fact(DisplayName = "EstaBloqueado retorna verdadeiro a partir da 5ª falha")]
    public void EstaBloqueado_DeveRetornarVerdadeiroComCincoFalhas()
    {
        for (var i = 0; i < 5; i++)
        {
            _limiter.RegistrarFalha(Email, Origem);
        }

        Assert.True(_limiter.EstaBloqueado(Email, Origem));
    }

    [Fact(DisplayName = "RegistrarSucesso zera o contador de falhas")]
    public void RegistrarSucesso_DeveZerarContador()
    {
        for (var i = 0; i < 5; i++)
        {
            _limiter.RegistrarFalha(Email, Origem);
        }

        _limiter.RegistrarSucesso(Email, Origem);

        Assert.False(_limiter.EstaBloqueado(Email, Origem));
    }

    [Fact(DisplayName = "O contador é isolado por combinação de e-mail e origem")]
    public void EstaBloqueado_NaoDeveMisturarEmailsOuOrigensDiferentes()
    {
        for (var i = 0; i < 5; i++)
        {
            _limiter.RegistrarFalha(Email, Origem);
        }

        Assert.False(_limiter.EstaBloqueado("outra@exemplo.com", Origem));
        Assert.False(_limiter.EstaBloqueado(Email, "192.168.0.1"));
    }
}
