using Icarus.Infrastructure.Security;

namespace Icarus.Infrastructure.UnitTests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact(DisplayName = "Hash gera valores diferentes para a mesma senha, por causa do salt aleatório")]
    public void Hash_DeveGerarHashesDiferentesParaAMesmaSenha()
    {
        var hash1 = _hasher.Hash("minhaSenha123");
        var hash2 = _hasher.Hash("minhaSenha123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact(DisplayName = "Verify retorna verdadeiro quando a senha informada é a correta")]
    public void Verify_DeveRetornarVerdadeiroParaSenhaCorreta()
    {
        var hash = _hasher.Hash("minhaSenha123");

        Assert.True(_hasher.Verify("minhaSenha123", hash));
    }

    [Fact(DisplayName = "Verify retorna falso quando a senha informada é incorreta")]
    public void Verify_DeveRetornarFalsoParaSenhaIncorreta()
    {
        var hash = _hasher.Hash("minhaSenha123");

        Assert.False(_hasher.Verify("senhaErrada", hash));
    }
}
