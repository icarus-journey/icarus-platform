using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Missoes;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Missoes;

public class CriarMissaoRequestValidatorTests
{
    private readonly CriarMissaoRequestValidator _validator = new();

    private static CriarMissaoRequest RequisicaoValida() => new(
        null, "Caminhada", "descrição", "benefício", Dificuldade.Facil, null, null, null, null, null);

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Título vazio é rejeitado")]
    public void Validate_DeveRejeitarTituloVazio()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Titulo = "" });

        resultado.ShouldHaveValidationErrorFor(r => r.Titulo);
    }

    [Fact(DisplayName = "Horário em formato inválido é rejeitado")]
    public void Validate_DeveRejeitarHorarioInvalido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Horario = "25:99" });

        resultado.ShouldHaveValidationErrorFor(r => r.Horario);
    }

    [Fact(DisplayName = "Horário omitido não gera erro")]
    public void Validate_DeveAceitarHorarioOmitido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Horario = null });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Recorrência semanal sem nenhum dia da semana informado é rejeitada (RF-04.4)")]
    public void Validate_DeveRejeitarSemanalSemDiasDaSemana()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Semanal, DiasSemana = [] });

        resultado.ShouldHaveValidationErrorFor(r => r.DiasSemana);
    }

    [Fact(DisplayName = "Recorrência semanal com dias da semana informados não gera erro")]
    public void Validate_DeveAceitarSemanalComDiasDaSemana()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Semanal, DiasSemana = [DayOfWeek.Monday] });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Recorrência diária não exige dias da semana")]
    public void Validate_DeveAceitarDiariaSemDiasDaSemana()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Diaria });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "DataFimRecorrencia no passado é rejeitada")]
    public void Validate_DeveRejeitarDataFimRecorrenciaNoPassado()
    {
        var ontem = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var resultado = _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Diaria, DataFimRecorrencia = ontem });

        resultado.ShouldHaveValidationErrorFor(r => r.DataFimRecorrencia);
    }

    [Fact(DisplayName = "DataFimRecorrencia de hoje em diante não gera erro")]
    public void Validate_DeveAceitarDataFimRecorrenciaFutura()
    {
        var amanha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var resultado = _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Diaria, DataFimRecorrencia = amanha });

        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
