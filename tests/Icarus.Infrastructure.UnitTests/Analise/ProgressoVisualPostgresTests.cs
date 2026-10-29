using System.Globalization;
using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConsultarProgresso;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class ProgressoVisualPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private sealed class Relogio : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-23T12:00:00Z", CultureInfo.InvariantCulture);
    }

    [FatoPostgres]
    public async Task SerieUsaOMesmoConjuntoDoResumoRespeitandoFusoTitularEIntervalo()
    {
        var titular = await banco.CriarUsuarioAsync(0);
        var outro = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        var usuario = await contexto.Usuarios.FindAsync(titular);
        usuario!.FusoHorario = "America/Sao_Paulo";
        void Concluir(Guid id, string instante)
        {
            var missao = new Missao { UsuarioId = id, Titulo = "Não entra no gráfico", Dificuldade = Dificuldade.Facil, Pontuacao = 30, Horario = DateTimeOffset.UtcNow };
            contexto.Missoes.Add(missao);
            contexto.Ocorrencias.Add(new() { Missao = missao, DataPrevista = new(2026, 10, 12), Status = StatusOcorrencia.Concluida,
                RealizadaEm = DateTimeOffset.Parse(instante, CultureInfo.InvariantCulture) });
        }
        Concluir(titular, "2026-10-12T02:59:59Z"); // Ainda domingo no fuso do titular.
        Concluir(titular, "2026-10-12T03:00:00Z");
        Concluir(titular, "2026-10-19T02:59:59Z"); // Ainda domingo da semana consultada.
        Concluir(titular, "2026-10-19T03:00:00Z");
        Concluir(outro, "2026-10-12T15:00:00Z");
        await contexto.SaveChangesAsync();
        var caso = new ConsultarProgressoUseCase(new UsuarioRepository(contexto), new IndicadoresPessoaisRepository(contexto), new Relogio());
        var resultado = await caso.ExecuteAsync(new(titular, titular), default);
        Assert.Equal("1.1.0", resultado.VersaoContrato);
        Assert.Equal(2, resultado.Indicadores!.Concluidas);
        Assert.Equal(resultado.Indicadores.Concluidas, resultado.Evolucao!.Pontos.Sum(p => p.Concluidas));
        Assert.Equal([1, 0, 0, 0, 0, 0, 1], resultado.Evolucao.Pontos.Select(p => p.Concluidas));
        Assert.DoesNotContain("Não entra", JsonSerializer.Serialize(resultado));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => caso.ExecuteAsync(new(outro, titular), default));
        var semDados = await new IndicadoresPessoaisRepository(contexto).CalcularProgressoAsync(Guid.NewGuid(), resultado.Periodo!, default);
        Assert.Null(semDados.Evolucao);
        Assert.Null(resultado.Indicadores.TaxaConclusao);
    }
}
