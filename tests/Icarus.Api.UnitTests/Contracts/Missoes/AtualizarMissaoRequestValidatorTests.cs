using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Missoes;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Missoes;

public class AtualizarMissaoRequestValidatorTests
{
    private readonly AtualizarMissaoRequestValidator _validator = new();

    private static AtualizarMissaoRequest RequisicaoValida() => new(
        "Caminhada", "descrição", "benefício", Dificuldade.Facil, 30, null, "07:00");

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Horário em formato inválido é rejeitado")]
    public void Validate_DeveRejeitarHorarioInvalido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Horario = "não é hora" });

        resultado.ShouldHaveValidationErrorFor(r => r.Horario);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void RejeitaDiaMensalInvalido(int dia)
    {
        _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Mensal, DiaDoMes = dia, VersaoEsperada = "versao" })
            .ShouldHaveValidationErrorFor(r => r.DiaDoMes);
    }

    [Fact]
    public void RecorrenteExigeVersaoEDiasValidosParaSemanal()
    {
        var invalida = RequisicaoValida() with { Recorrencia = TipoRecorrencia.Semanal, DiasSemana = [] };
        var resultado = _validator.TestValidate(invalida);
        resultado.ShouldHaveValidationErrorFor(r => r.VersaoEsperada);
        resultado.ShouldHaveValidationErrorFor(r => r.DiasSemana);
        _validator.TestValidate(invalida with { DiasSemana = [(DayOfWeek)9], VersaoEsperada = "versao" })
            .ShouldHaveValidationErrorFor("DiasSemana[0]");
    }

    [Fact]
    public void AceitaConfiguracaoMensalVersionadaSemDataFinal()
    {
        _validator.TestValidate(RequisicaoValida() with { Recorrencia = TipoRecorrencia.Mensal, DiaDoMes = 31, VersaoEsperada = "versao" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void RejeitaRecompensaForaDoIntervalo(int pontuacao)
    {
        _validator.TestValidate(RequisicaoValida() with { Pontuacao = pontuacao })
            .ShouldHaveValidationErrorFor(r => r.Pontuacao);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(137)]
    [InlineData(1_000_000)]
    public void AceitaRecompensaIndependenteDaDificuldade(int pontuacao)
    {
        _validator.TestValidate(RequisicaoValida() with { Pontuacao = pontuacao, Dificuldade = Dificuldade.Trivial })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CorpoSemPontuacaoNaoRecebeValorPelaDificuldade()
    {
        var opcoes = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        var campos = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(RequisicaoValida(), opcoes))!.AsObject();
        campos.Remove("pontuacao");
        var requisicao = System.Text.Json.JsonSerializer.Deserialize<AtualizarMissaoRequest>(campos.ToJsonString(), opcoes)!;
        _validator.TestValidate(requisicao).ShouldHaveValidationErrorFor(r => r.Pontuacao);
    }

}
