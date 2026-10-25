using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarProgressoCampanha;
using Icarus.Application.UseCases.RegistrarProgressoCampanha;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class ProgressoCampanhaPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static ConsultarProgressoCampanhaUseCase Consulta(IcarusDbContext c) => new(new CampanhaRepository(c), new MedicaoCampanhaRepository(c));
    private static RegistrarProgressoCampanhaUseCase Registro(IcarusDbContext c) => new(new CampanhaRepository(c), new MedicaoCampanhaRepository(c), new UsuarioRepository(c), c, TimeProvider.System, NullLogger<RegistrarProgressoCampanhaUseCase>.Instance);

    private async Task<(Guid Usuario, long Id)> PrepararAsync()
    {
        var usuario = await banco.CriarUsuarioAsync(500);
        await using var c = banco.NovoContexto();
        var pessoa = await c.Usuarios.SingleAsync(u => u.Id == usuario);
        pessoa.FusoHorario = "America/Sao_Paulo";
        var epico = new Epico { UsuarioId = usuario, Titulo = "Estudos", AreaDaVida = AreaDaVida.EstudosEAprendizado };
        var campanha = new Campanha { Epico = epico, Titulo = "Curso", AreaDaVida = epico.AreaDaVida, ValorAtual = 2,
            ValorMensurado = 12, Unidade = "módulos", Estado = EstadoCampanha.EmAndamento };
        c.Campanhas.Add(campanha);
        await c.SaveChangesAsync();
        return (usuario, campanha.Id);
    }

    [FatoPostgres]
    public async Task PreservaLegadoRegistraAumentoReducaoEExcedenteSemConcluirOuDarPontos()
    {
        var (usuario, id) = await PrepararAsync();
        await using (var c = banco.NovoContexto())
        {
            var antes = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            Assert.Empty(antes.Medicoes);
            Assert.Equal(2m, antes.ValorAtual);
            foreach (var valor in new[] { 5.5m, 3m, 14m })
            {
                var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
                var salvo = await Registro(c).ExecuteAsync(new(usuario, usuario, id, valor, atual.Versao, Guid.NewGuid()), default);
                Assert.Equal(valor, salvo.ValorAtual);
            }
        }
        await using var novo = banco.NovoContexto();
        var resultado = await Consulta(novo).ExecuteAsync(new(usuario, usuario, id), default);
        Assert.Equal(3, resultado.Medicoes.Count);
        Assert.Collection(resultado.Medicoes, m => Assert.Equal(14m, m.ValorAtual), m => Assert.Equal(3m, m.ValorAtual), m => Assert.Equal(5.5m, m.ValorAtual));
        Assert.Equal(2m, resultado.Medicoes[^1].ValorAnterior);
        Assert.All(resultado.Medicoes, m => { Assert.Equal(12m, m.ValorMensurado); Assert.Equal("módulos", m.Unidade); Assert.Equal("America/Sao_Paulo", m.FusoHorario); Assert.NotEqual(default, m.RegistradaEm); });
        Assert.True(resultado.MetaAlcancada);
        Assert.Equal(EstadoCampanha.EmAndamento, resultado.Estado);
        Assert.Equal(500, (await novo.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
        Assert.Empty(await novo.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task RepetirTentativaNaoDuplicaNemReverteMedicaoMaisRecente()
    {
        var (usuario, id) = await PrepararAsync();
        var operacao = Guid.NewGuid();
        string versao;
        await using (var c = banco.NovoContexto())
        {
            versao = (await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default)).Versao;
            await Registro(c).ExecuteAsync(new(usuario, usuario, id, 5, versao, operacao), default);
        }
        await using (var c = banco.NovoContexto())
        {
            var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Registro(c).ExecuteAsync(new(usuario, usuario, id, 6, atual.Versao, Guid.NewGuid()), default);
        }
        await using var repeticao = banco.NovoContexto();
        var resultado = await Registro(repeticao).ExecuteAsync(new(usuario, usuario, id, 5, versao, operacao), default);
        Assert.Equal(6m, resultado.ValorAtual);
        Assert.Equal(2, resultado.Medicoes.Count);
        await Assert.ThrowsAsync<ProgressoCampanhaException>(() => Registro(repeticao).ExecuteAsync(new(usuario, usuario, id, 7, versao, operacao), default));
    }

    [FatoPostgres]
    public async Task IsolaTitularidadeENaoAceitaCampanhaSemMetaOuEncerrada()
    {
        var (usuario, id) = await PrepararAsync();
        var outro = await banco.CriarUsuarioAsync(0);
        await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => Consulta(c).ExecuteAsync(new(outro, usuario, id), default));
        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => Consulta(c).ExecuteAsync(new(outro, outro, id), default));
        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => Registro(c).ExecuteAsync(new(outro, outro, id, 3, "versao", Guid.NewGuid()), default));
        var campanha = await c.Campanhas.SingleAsync(x => x.Id == id);
        campanha.Unidade = null;
        await c.SaveChangesAsync();
        Assert.False((await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default)).PodeRegistrar);
        await Assert.ThrowsAsync<ProgressoCampanhaException>(() => Registro(c).ExecuteAsync(new(usuario, usuario, id, 3, "versao", Guid.NewGuid()), default));
        campanha.Unidade = "módulos";
        campanha.Estado = EstadoCampanha.Concluida;
        await c.SaveChangesAsync();
        await Assert.ThrowsAsync<ProgressoCampanhaException>(() => Registro(c).ExecuteAsync(new(usuario, usuario, id, 3, "versao", Guid.NewGuid()), default));
        Assert.Empty(await c.MedicoesCampanhas.Where(m => m.CampanhaId == id).ToListAsync());
    }

    [FatoPostgres]
    public async Task VersaoDeRespostaPodeSerUsadaImediatamenteEmOutroContexto()
    {
        var (usuario, id) = await PrepararAsync();
        string versao;
        await using (var c = banco.NovoContexto())
        {
            var antes = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            versao = (await Registro(c).ExecuteAsync(new(usuario, usuario, id, 3, antes.Versao, Guid.NewGuid()), default)).Versao;
        }
        await using var outro = banco.NovoContexto();
        var resposta = await Registro(outro).ExecuteAsync(new(usuario, usuario, id, 3, versao, Guid.NewGuid()), default);
        Assert.Equal(3m, resposta.ValorAtual);
        Assert.Equal(2, resposta.Medicoes.Count);
    }

    [FatoPostgres]
    public async Task EdicaoConcorrenteDeCampanhaOuMedicaoNaoSobrescreveResultado()
    {
        var (usuario, id) = await PrepararAsync();
        await using var antigo = banco.NovoContexto();
        var antes = await Consulta(antigo).ExecuteAsync(new(usuario, usuario, id), default);
        await using (var novo = banco.NovoContexto())
            await Registro(novo).ExecuteAsync(new(usuario, usuario, id, 8, antes.Versao, Guid.NewGuid()), default);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Registro(antigo).ExecuteAsync(new(usuario, usuario, id, 4, antes.Versao, Guid.NewGuid()), default));
        await using var leitura = banco.NovoContexto();
        var depois = await Consulta(leitura).ExecuteAsync(new(usuario, usuario, id), default);
        Assert.Equal(8m, depois.ValorAtual);
        Assert.Single(depois.Medicoes);
    }

    [FatoPostgres]
    public async Task DuasGravacoesSimultaneasGeramSomenteUmRegistro()
    {
        var (usuario, id) = await PrepararAsync();
        await using var leitura = banco.NovoContexto();
        var antes = await Consulta(leitura).ExecuteAsync(new(usuario, usuario, id), default);
        async Task<bool> Tentar(decimal valor)
        {
            await using var c = banco.NovoContexto();
            try { await Registro(c).ExecuteAsync(new(usuario, usuario, id, valor, antes.Versao, Guid.NewGuid()), default); return true; }
            catch (DadosAtualizadosException) { return false; }
        }
        var resultados = await Task.WhenAll(Tentar(3), Tentar(4));
        Assert.Single(resultados, x => x);
        await using var final = banco.NovoContexto();
        var depois = await Consulta(final).ExecuteAsync(new(usuario, usuario, id), default);
        Assert.Single(depois.Medicoes);
        Assert.Equal(depois.ValorAtual, depois.Medicoes[0].ValorAtual);
    }

    [FatoPostgres]
    public async Task HistoricoNaoPodeSerAlteradoOuApagadoESnapshotNaoMudaComMeta()
    {
        var (usuario, id) = await PrepararAsync();
        await using var c = banco.NovoContexto();
        var antes = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
        await Registro(c).ExecuteAsync(new(usuario, usuario, id, 3, antes.Versao, Guid.NewGuid()), default);
        c.ChangeTracker.Clear();
        var campanha = await c.Campanhas.SingleAsync(x => x.Id == id);
        campanha.ValorMensurado = 20;
        campanha.Unidade = "aulas";
        await c.SaveChangesAsync();
        var salvo = await c.MedicoesCampanhas.SingleAsync(m => m.CampanhaId == id);
        Assert.Equal(12m, salvo.ValorMensurado);
        Assert.Equal("módulos", salvo.Unidade);
        c.Entry(salvo).Property(m => m.ValorAtual).CurrentValue = 99;
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        c.ChangeTracker.Clear();
        c.MedicoesCampanhas.Remove(await c.MedicoesCampanhas.SingleAsync(m => m.CampanhaId == id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
    }
}
