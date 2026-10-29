using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.AtualizarEntradaDiario;
using Icarus.Application.UseCases.CriarEntradaDiario;
using Icarus.Application.UseCases.ExcluirEntradaDiario;
using Icarus.Application.UseCases.ListarEntradasDiario;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Diario;

public sealed class DiarioPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static CriarEntradaDiarioUseCase Criacao(IcarusDbContext c) =>
        new(new DiarioRepository(c), new UsuarioRepository(c), c, NullLogger<CriarEntradaDiarioUseCase>.Instance);
    private static ListarEntradasDiarioUseCase Leitura(IcarusDbContext c) =>
        new(new DiarioRepository(c), new UsuarioRepository(c));
    private static AtualizarEntradaDiarioUseCase Edicao(IcarusDbContext c) =>
        new(new DiarioRepository(c), c, NullLogger<AtualizarEntradaDiarioUseCase>.Instance);
    private static ExcluirEntradaDiarioUseCase Exclusao(IcarusDbContext c) =>
        new(new DiarioRepository(c), c, TimeProvider.System, NullLogger<ExcluirEntradaDiarioUseCase>.Instance);

    [FatoPostgres]
    public async Task PreservaHistoricoTipoDataETextoEntreContextosSemDarPontos()
    {
        var usuario = await banco.CriarUsuarioAsync(300);
        long id;
        await using (var c = banco.NovoContexto())
        {
            var primeira = await Criacao(c).ExecuteAsync(new(usuario, usuario, TipoEntradaDiario.Reflexao, new(2026, 8, 1), "  Estudei um módulo.  "), default);
            id = primeira.Id;
            await Criacao(c).ExecuteAsync(new(usuario, usuario, TipoEntradaDiario.Deslize, new(2026, 10, 1), "Reorganizar o horário."), default);
            await Criacao(c).ExecuteAsync(new(usuario, usuario, TipoEntradaDiario.Reflexao, new(2026, 10, 1), "Retomei."), default);
        }
        await using (var c = banco.NovoContexto())
        {
            var entradas = await Leitura(c).ExecuteAsync(new(usuario, usuario), default);
            Assert.Equal(3, entradas.Count);
            Assert.Equal(new DateOnly(2026, 10, 1), entradas[0].DataRegistro);
            Assert.Equal("Estudei um módulo.", entradas[^1].Texto);
            Assert.NotEqual(default, entradas[^1].CriadaEm);
            await Edicao(c).ExecuteAsync(new(usuario, usuario, id, TipoEntradaDiario.Deslize, new(2026, 8, 1), "  Ajustei meu registro.  "), default);
        }
        await using var final = banco.NovoContexto();
        var salvo = await final.Diarios.SingleAsync(e => e.Id == id);
        Assert.Equal("Ajustei meu registro.", salvo.Texto);
        Assert.Equal(new DateOnly(2026, 8, 1), salvo.DataRegistro);
        Assert.Equal(TipoEntradaDiario.Deslize, salvo.Tipo);
        Assert.Equal(300, (await final.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
        Assert.Empty(await final.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task ExcluiLogicamenteDeFormaRepetivelESemPermitirEdicaoPosterior()
    {
        var usuario = await banco.CriarUsuarioAsync(0);
        await using var c = banco.NovoContexto();
        var entrada = await Criacao(c).ExecuteAsync(new(usuario, usuario, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Registro"), default);
        await Exclusao(c).ExecuteAsync(new(usuario, usuario, entrada.Id), default);
        var data = (await c.Diarios.SingleAsync(e => e.Id == entrada.Id)).ExcluidaEm;
        await Exclusao(c).ExecuteAsync(new(usuario, usuario, entrada.Id), default);
        Assert.Equal(data, (await c.Diarios.SingleAsync(e => e.Id == entrada.Id)).ExcluidaEm);
        Assert.Empty(await Leitura(c).ExecuteAsync(new(usuario, usuario), default));
        await Assert.ThrowsAsync<EntradaDiarioNaoEncontradaException>(() => Edicao(c).ExecuteAsync(
            new(usuario, usuario, entrada.Id, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Novo"), default));
    }

    [FatoPostgres]
    public async Task IsolaTitularidadeEmTodosOsCasos()
    {
        var usuario = await banco.CriarUsuarioAsync(0);
        var outro = await banco.CriarUsuarioAsync(0);
        await using var c = banco.NovoContexto();
        var entrada = await Criacao(c).ExecuteAsync(new(usuario, usuario, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Privado"), default);
        Assert.Empty(await Leitura(c).ExecuteAsync(new(outro, outro), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Leitura(c).ExecuteAsync(new(outro, usuario), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Criacao(c).ExecuteAsync(new(outro, usuario, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Outro"), default));
        await Assert.ThrowsAsync<EntradaDiarioNaoEncontradaException>(() => Edicao(c).ExecuteAsync(new(outro, outro, entrada.Id, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Outro"), default));
        await Assert.ThrowsAsync<EntradaDiarioNaoEncontradaException>(() => Exclusao(c).ExecuteAsync(new(outro, outro, entrada.Id), default));
        var ausente = Guid.NewGuid();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Leitura(c).ExecuteAsync(new(ausente, ausente), default));
        Assert.Equal("Privado", (await c.Diarios.SingleAsync(e => e.Id == entrada.Id)).Texto);
    }

    [FatoPostgres]
    public async Task ExclusaoConcorrenteNaoPermiteRessuscitarOuSobrescreverEntrada()
    {
        var usuario = await banco.CriarUsuarioAsync(0);
        await using var antiga = banco.NovoContexto();
        var entrada = await Criacao(antiga).ExecuteAsync(new(usuario, usuario, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Original"), default);
        await using (var nova = banco.NovoContexto()) await Exclusao(nova).ExecuteAsync(new(usuario, usuario, entrada.Id), default);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Edicao(antiga).ExecuteAsync(new(usuario, usuario, entrada.Id, TipoEntradaDiario.Reflexao, new(2026, 10, 20), "Desatualizado"), default));
        await using var final = banco.NovoContexto();
        var salvo = await final.Diarios.SingleAsync(e => e.Id == entrada.Id);
        Assert.NotNull(salvo.ExcluidaEm);
        Assert.Equal("Original", salvo.Texto);
    }
}
