using Icarus.Application.Common;
using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
namespace Icarus.Infrastructure.UnitTests.HistoricoMissoes;

public sealed class HistoricoMissoesPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = new(2027, 1, 20, 2, 0, 0, TimeSpan.Zero);
    private static readonly FiltroHistoricoMissoes Filtro = new(new(2027, 1, 1), new(2027, 1, 31), EstadoHistoricoMissao.Todos, null, false, 20);
    private async Task<(Guid Usuario, long Campanha, long[] Ocorrencias)> Preparar()
    {
        var id = await banco.CriarUsuarioAsync(100);
        await using var c = banco.NovoContexto();
        var epico = new Epico
        {
            UsuarioId = id,
            Titulo = "Épico preservado",
            Estado = EstadoEpico.Excluido,
            Campanhas = [new() { Titulo = "Curso removido", Estado = EstadoCampanha.Excluida }]
        };
        c.Epicos.Add(epico); await c.SaveChangesAsync(); var campanha = epico.Campanhas.Single();
        var ocs = new List<Ocorrencia>();
        foreach (var status in Enum.GetValues<StatusOcorrencia>())
        {
            var m = new Missao { UsuarioId = id, CampanhaId = campanha.Id, Titulo = "Nome preservado", Status = StatusMissao.Excluida, Pontuacao = 999 };
            var o = new Ocorrencia { Missao = m, DataPrevista = new(2027, 1, 19), Status = status, Planejamento = PlanejamentoOcorrencia.Capturar(m) };
            m.Titulo = "Nome atual"; c.Ocorrencias.Add(o); ocs.Add(o);
            if (status == StatusOcorrencia.Concluida) o.MovimentacoesPontos.Add(new() { UsuarioId = id, Tipo = TipoMovimentacaoPontos.GanhoMissao, Quantidade = 30, CriadoEm = DateTimeOffset.UtcNow });
        }
        await c.SaveChangesAsync(); return (id, campanha.Id, ocs.Select(o => o.Id).ToArray());
    }
    [FatoPostgres]
    public async Task PreservaHistoricoExcluidoIsolaTitularENaoMudaSaldoOuPlanejamento()
    {
        var p = await Preparar(); var outro = await Preparar(); await using var c = banco.NovoContexto(); var repo = new HistoricoMissoesRepository(c);
        var antes = await c.Set<RegistroPlanejamentoOcorrencia>().CountAsync();
        var r = await repo.ListarAsync(p.Usuario, Filtro, Agora, "UTC", null, default);
        Assert.Equal(5, r.Registros.Count); Assert.All(r.Registros, x => { Assert.Contains(x.Id, p.Ocorrencias); Assert.Equal("Nome preservado", x.Titulo); Assert.True(x.MissaoRemovida); Assert.True(x.Campanha!.Removida); });
        Assert.Equal(30, r.Registros.Single(x => x.Estado == EstadoHistoricoMissao.Feita).PontosRecebidos);
        Assert.Equal(100, (await c.Usuarios.FindAsync(p.Usuario))!.SaldoPontos); Assert.Equal(antes, await c.Set<RegistroPlanejamentoOcorrencia>().CountAsync());
        Assert.Null(await repo.ConsultarAsync(outro.Usuario, p.Ocorrencias[0], new(2027, 1, 20), default));
        Assert.All(await repo.ListarCampanhasAsync(p.Usuario, default), x => Assert.Equal(p.Campanha, x.Id));
    }
    [FatoPostgres]
    public async Task CursorOrdenaPorDataEIdSemDuplicarEmpatesOuIncluirNovaLinha()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto(); var repo = new HistoricoMissoesRepository(c); var filtro = Filtro with { Limite = 2 };
        var a = await repo.ListarAsync(p.Usuario, filtro, Agora, "UTC", null, default); Assert.True(a.TemMais);
        c.Ocorrencias.Add(new() { Missao = new() { UsuarioId = p.Usuario, Titulo = "Inserida depois" }, DataPrevista = new(2027, 1, 19) }); await c.SaveChangesAsync();
        var ultimo = a.Registros[^1]; var cursor = new CursorHistoricoMissoes(p.Usuario, filtro, ultimo.DataPrevista, ultimo.Id, a.LimiteId, Agora, "UTC");
        var b = await repo.ListarAsync(p.Usuario, filtro, Agora, "UTC", cursor, default); ultimo = b.Registros[^1];
        var d = await repo.ListarAsync(p.Usuario, filtro, Agora, "UTC", cursor with { UltimaData = ultimo.DataPrevista, UltimoId = ultimo.Id }, default);
        var ids = a.Registros.Concat(b.Registros).Concat(d.Registros).Select(x => x.Id).ToArray();
        Assert.Equal(p.Ocorrencias.OrderDescending(), ids); Assert.False(d.TemMais); Assert.Equal(5, ids.Distinct().Count());
    }
    [FatoPostgres]
    public async Task EstadoDoDiaRespeitaFusoENaoPrecisaRotinaDeEscrita()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto(); var repo = new HistoricoMissoesRepository(c);
        var pendentes = Filtro with { Estado = EstadoHistoricoMissao.Pendente };
        var br = await repo.ListarAsync(p.Usuario, pendentes, Agora, "America/Sao_Paulo", null, default);
        var utc = await repo.ListarAsync(p.Usuario, pendentes, Agora, "UTC", null, default);
        Assert.Equal(2, br.Registros.Count); Assert.Empty(utc.Registros);
        var atrasadas = await repo.ListarAsync(p.Usuario, Filtro with { Estado = EstadoHistoricoMissao.Atrasada }, Agora, "UTC", null, default);
        Assert.Equal(2, atrasadas.Registros.Count);
    }
    [FatoPostgres]
    public async Task FiltraDataPrevistaCampanhaAvulsasEEstadosSemInferirRealizacao()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto(); var repo = new HistoricoMissoesRepository(c);
        var o = new Ocorrencia { Missao = new() { UsuarioId = p.Usuario, Titulo = "Avulsa" }, DataPrevista = new(2026, 12, 31), Status = StatusOcorrencia.Concluida, RealizadaEm = Agora }; c.Ocorrencias.Add(o); await c.SaveChangesAsync();
        Assert.Empty((await repo.ListarAsync(p.Usuario, Filtro with { Avulsas = true }, Agora, "UTC", null, default)).Registros);
        Assert.Single((await repo.ListarAsync(p.Usuario, Filtro with { CampanhaId = p.Campanha, Estado = EstadoHistoricoMissao.FeitaComAtraso }, Agora, "UTC", null, default)).Registros);
        var legada = await repo.ConsultarAsync(p.Usuario, o.Id, new(2027, 1, 20), default); Assert.NotNull(legada); Assert.Null(legada.PontosRecebidos); Assert.False(legada.TituloPreservado);
    }
    [FatoPostgres]
    public async Task EspelhoSemOcorrenciaInformaHistoricoParcialSemFabricarRegistros()
    {
        var id = await banco.CriarUsuarioAsync(100); await using var c = banco.NovoContexto(); c.Missoes.Add(new() { UsuarioId = id, Titulo = "Legado", ConcluidaEm = DateTimeOffset.UtcNow }); await c.SaveChangesAsync();
        var r = await new HistoricoMissoesRepository(c).ListarAsync(id, Filtro, Agora, "UTC", null, default);
        Assert.True(r.HistoricoParcial); Assert.Empty(r.Registros);
    }
}
