using System.Data.Common;
using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class ObjetivosPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly PeriodoAnalitico Periodo = new(new(2027, 1, 4), new(2027, 1, 10), "UTC");
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2027, 1, 20, 12, 0, 0, TimeSpan.Zero); }
    private static readonly Relogio Hora = new();
    private async Task<(Guid Usuario, long Epico, long Campanha)> Preparar()
    {
        var id = await banco.CriarUsuarioAsync(100);
        await using var c = banco.NovoContexto();
        var e = new Epico { UsuarioId = id, Titulo = "Objetivo privado", Campanhas = [new() { Titulo = "Curso privado", Estado = EstadoCampanha.EmAndamento, ValorAtual = 8, ValorMensurado = 12, Unidade = "módulos" }] };
        c.Epicos.Add(e); await c.SaveChangesAsync();
        var campanha = e.Campanhas.Single();
        foreach (var dia in new[] { 6, 19 })
        {
            c.Ocorrencias.Add(new()
            {
                Missao = new() { UsuarioId = id, CampanhaId = campanha.Id, Titulo = "Título que não entra na IA", Status = StatusMissao.Excluida },
                DataPrevista = new(2027, 1, dia),
                Status = StatusOcorrencia.Concluida,
                RealizadaEm = new(2027, 1, dia, 12, 0, 0, TimeSpan.Zero),
                ConcluidaEm = new(2027, 1, dia, 12, 0, 0, TimeSpan.Zero)
            });
        }
        foreach (var (dia, valor) in new[] { (3, 2), (6, 4), (9, 6), (19, 8) })
            c.MedicoesCampanhas.Add(new() { CampanhaId = campanha.Id, ValorAtual = valor, ValorMensurado = 12, Unidade = "módulos", FusoHorario = "UTC", RegistradaEm = new(2027, 1, dia, 12, 0, 0, TimeSpan.Zero), OperacaoId = Guid.NewGuid() });
        await c.SaveChangesAsync();
        return (id, e.Id, campanha.Id);
    }

    [FatoPostgres]
    public async Task IsolaTitularContaMissoesPreservadasEUsaMedicoesReais()
    {
        var p = await Preparar(); var outro = await Preparar();
        await using var c = banco.NovoContexto(); var repo = new IndicadoresPessoaisRepository(c, Hora);
        var antes = await c.Set<RegistroPlanejamentoOcorrencia>().CountAsync();
        var r = await repo.CalcularProgressoAsync(p.Usuario, Periodo, default);
        var objetivos = r.Objetivos!; var campanha = Assert.Single(objetivos.Campanhas);
        Assert.Equal(p.Campanha, campanha.Id); Assert.NotEqual(outro.Campanha, campanha.Id);
        Assert.Equal(1, campanha.Atividade.ConcluidasNoPeriodo); Assert.Equal(1, campanha.Atividade.ConcluidasNosUltimosSeteDias);
        Assert.Equal(4, campanha.Resultado.EvolucaoNoPeriodo.AvancoRegistrado); Assert.Equal(8, campanha.Resultado.ValorAtual);
        Assert.Equal(p.Campanha, objetivos.CampanhaEmDestaqueId);
        Assert.Equal(antes, await c.Set<RegistroPlanejamentoOcorrencia>().CountAsync());
        Assert.Equal(100, (await c.Usuarios.FindAsync(p.Usuario))!.SaldoPontos);
        var relatorio = await repo.CalcularAsync(p.Usuario, Periodo, default);
        var carga = JsonSerializer.Serialize(IndicadoresRelatorioSemanal.DosIndicadores(relatorio), ContratoRelatorioSemanal.Json);
        Assert.DoesNotContain("Curso privado", carga); Assert.DoesNotContain("Objetivo privado", carga);
        Assert.Null(relatorio.ProgressoManualCampanhas);
    }

    [FatoPostgres]
    public async Task TrocarPeriodoAlteraAtividadeEResultadoMasNaoEstadosOuValoresAtuais()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto(); var repo = new IndicadoresPessoaisRepository(c, Hora);
        var a = (await repo.CalcularProgressoAsync(p.Usuario, Periodo, default)).Objetivos!;
        var b = (await repo.CalcularProgressoAsync(p.Usuario, new(new(2026, 12, 1), new(2026, 12, 31), "UTC"), default)).Objetivos!;
        Assert.Equal(a.CampanhasEmAndamento, b.CampanhasEmAndamento); Assert.Equal(a.CampanhaEmDestaqueId, b.CampanhaEmDestaqueId);
        Assert.Equal(a.Campanhas[0].Resultado.ValorAtual, b.Campanhas[0].Resultado.ValorAtual);
        Assert.Equal(0, b.Campanhas[0].Atividade.ConcluidasNoPeriodo); Assert.Null(b.Campanhas[0].Resultado.EvolucaoNoPeriodo.AvancoRegistrado);
    }

    [FatoPostgres]
    public async Task ExclusaoNaoCriaLinksParaItensRemovidosENaoApagaAtividadeDoEpico()
    {
        var p = await Preparar();
        await using (var c = banco.NovoContexto()) { (await c.Campanhas.FindAsync(p.Campanha))!.Estado = EstadoCampanha.Excluida; await c.SaveChangesAsync(); }
        await using var leitura = banco.NovoContexto();
        var r = (await new IndicadoresPessoaisRepository(leitura, Hora).CalcularProgressoAsync(p.Usuario, Periodo, default)).Objetivos!;
        Assert.Empty(r.Campanhas); Assert.Single(r.Epicos); Assert.Equal(1, r.Epicos[0].Atividade.ConcluidasNoPeriodo);
        Assert.Equal(0, r.Epicos[0].Campanhas.EmAndamento); Assert.Null(r.CampanhaEmDestaqueId);
    }

    [FatoPostgres]
    public async Task EspelhoLegadoSemRealizacaoImpedeAvisoDeCampanhaSemRegistro()
    {
        var p = await Preparar();
        await using (var c = banco.NovoContexto()) { c.Missoes.Add(new() { UsuarioId = p.Usuario, CampanhaId = p.Campanha, Titulo = "Legado", ConcluidaEm = Hora.GetUtcNow() }); await c.SaveChangesAsync(); }
        await using var leitura = banco.NovoContexto();
        var r = (await new IndicadoresPessoaisRepository(leitura, Hora).CalcularProgressoAsync(p.Usuario, Periodo, default)).Objetivos!;
        Assert.Null(r.CampanhasSemRegistroRecente); Assert.Null(r.CampanhaEmDestaqueId);
        Assert.Equal("historico_incompleto", r.EstadoDestaque); Assert.Null(r.Campanhas[0].Atividade.ConcluidasNosUltimosSeteDias);
    }

    [FatoPostgres]
    public async Task SnapshotNaoMisturaValorAtualOuEstadoDepoisDeAlteracaoConcorrente()
    {
        var p = await Preparar();
        async Task Alterar()
        {
            await using var c = banco.NovoContexto(); var campanha = (await c.Campanhas.FindAsync(p.Campanha))!;
            campanha.ValorAtual = 10; campanha.Estado = EstadoCampanha.Concluida; await c.SaveChangesAsync();
        }
        var opcoes = new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().AddInterceptors(new AlteracaoDuranteLeitura(Alterar)).Options;
        await using var leitura = new IcarusDbContext(opcoes);
        var repo = new IndicadoresPessoaisRepository(leitura, Hora);
        var a = (await repo.CalcularProgressoAsync(p.Usuario, Periodo, default)).Objetivos!;
        Assert.Equal(8, a.Campanhas[0].Resultado.ValorAtual); Assert.Equal(1, a.CampanhasEmAndamento);
        var b = (await repo.CalcularProgressoAsync(p.Usuario, Periodo, default)).Objetivos!;
        Assert.Equal(10, b.Campanhas[0].Resultado.ValorAtual); Assert.Equal(0, b.CampanhasEmAndamento);
    }

    private sealed class AlteracaoDuranteLeitura(Func<Task> alterar) : DbCommandInterceptor
    {
        private bool _executada;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!_executada && command.CommandText.Contains("FROM ocorrencia AS o", StringComparison.Ordinal)) { _executada = true; await alterar(); }
            return result;
        }
    }
}
