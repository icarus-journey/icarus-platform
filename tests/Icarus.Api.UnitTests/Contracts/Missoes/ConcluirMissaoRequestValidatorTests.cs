using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Missoes;

namespace Icarus.Api.UnitTests.Contracts.Missoes;

public sealed class ConcluirMissaoRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(1L)]
    public void AceitaExecucaoIdentificadaOuContratoLegado(long? id)
        => new ConcluirMissaoRequestValidator().TestValidate(new ConcluirMissaoRequest(id)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void RejeitaIdentificadorInvalido(long id)
        => new ConcluirMissaoRequestValidator().TestValidate(new ConcluirMissaoRequest(id)).ShouldHaveValidationErrorFor(r => r.OcorrenciaId);
    [Fact]
    public void RealizacaoExigeDataEHorarioJuntos()
    {
        var validador = new ConcluirMissaoRequestValidator();
        validador.TestValidate(new ConcluirMissaoRequest(DataRealizacao: new(2026, 10, 30))).ShouldHaveValidationErrorFor(r => r.HorarioRealizacao);
        validador.TestValidate(new ConcluirMissaoRequest(HorarioRealizacao: new(20, 0))).ShouldHaveValidationErrorFor(r => r.DataRealizacao);
        validador.TestValidate(new ConcluirMissaoRequest(DataRealizacao: new(2026, 10, 30), HorarioRealizacao: new(20, 0))).ShouldNotHaveAnyValidationErrors();
    }

}
