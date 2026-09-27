using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Icarus.Application.Abstractions;
using Konscious.Security.Cryptography;

namespace Icarus.Infrastructure.Security;

/// <summary>
/// Hash de senha com Argon2id (RNF-12), vencedor da Password Hashing
/// Competition e recomendação atual da OWASP. O hash é guardado como uma
/// única string autodescritiva (parâmetros + salt + hash, formato inspirado
/// no PHC string format), permitindo no futuro aumentar o custo sem quebrar
/// hashes antigos já salvos.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int TamanhoSaltEmBytes = 16;
    private const int TamanhoHashEmBytes = 32;

    // RFC 9106, segunda opção recomendada (para quando 2 GiB de memória não estão disponíveis).
    private const int MemoriaEmKb = 65536;
    private const int Iteracoes = 3;
    private const int GrauDeParalelismo = 4;

    public string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSaltEmBytes);
        var hash = CalcularHash(senha, salt, MemoriaEmKb, Iteracoes, GrauDeParalelismo, TamanhoHashEmBytes);

        return $"$argon2id$v=19$m={MemoriaEmKb},t={Iteracoes},p={GrauDeParalelismo}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string senha, string hash)
    {
        var partes = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length != 5 || partes[0] != "argon2id")
        {
            return false;
        }

        var parametros = partes[2].Split(',');
        var memoriaEmKb = int.Parse(parametros[0].Split('=')[1], CultureInfo.InvariantCulture);
        var iteracoes = int.Parse(parametros[1].Split('=')[1], CultureInfo.InvariantCulture);
        var grauDeParalelismo = int.Parse(parametros[2].Split('=')[1], CultureInfo.InvariantCulture);
        var salt = Convert.FromBase64String(partes[3]);
        var hashEsperado = Convert.FromBase64String(partes[4]);

        var hashCalculado = CalcularHash(senha, salt, memoriaEmKb, iteracoes, grauDeParalelismo, hashEsperado.Length);

        // Comparação em tempo constante: evita vazar, pelo tempo de resposta, em qual byte o hash divergiu.
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }

    private static byte[] CalcularHash(string senha, byte[] salt, int memoriaEmKb, int iteracoes, int grauDeParalelismo, int tamanhoHashEmBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(senha))
        {
            Salt = salt,
            DegreeOfParallelism = grauDeParalelismo,
            MemorySize = memoriaEmKb,
            Iterations = iteracoes
        };

        return argon2.GetBytes(tamanhoHashEmBytes);
    }
}
