using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Api.Tests;

public class OcorrenciaTests
{
    [Fact]
    public void NovaOcorrenciaIniciaComoPendenteESemMovimentacoes()
    {
        var ocorrencia = new Ocorrencia();

        Assert.Equal(StatusOcorrencia.Pendente, ocorrencia.Status);
        Assert.Empty(ocorrencia.MovimentacoesPontos);
    }
}
