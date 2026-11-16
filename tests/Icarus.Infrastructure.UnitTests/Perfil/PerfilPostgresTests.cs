using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.AtualizarFuso;
using Icarus.Application.UseCases.AtualizarPerfil;
using Icarus.Application.UseCases.ObterPerfil;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Perfil;

// Reutiliza a infraestrutura de banco descartável dos testes da loja.
public sealed class PerfilPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static AtualizarPerfilUseCase Edicao(IcarusDbContext contexto) =>
        new(new UsuarioRepository(contexto), contexto, NullLogger<AtualizarPerfilUseCase>.Instance);
    private static AtualizarFusoUseCase Fuso(IcarusDbContext contexto) =>
        new(new UsuarioRepository(contexto), contexto, NullLogger<AtualizarFusoUseCase>.Instance);
    private static ObterPerfilUseCase Consulta(IcarusDbContext contexto) =>
        new(new UsuarioRepository(contexto), NullLogger<ObterPerfilUseCase>.Instance);

    [FatoPostgres]
    public async Task PersisteConsultaELimpaOpcionaisSemMudarCadastroSaldoOuFuso()
    {
        var id = await banco.CriarUsuarioAsync(300);
        await using (var contexto = banco.NovoContexto())
        {
            var inicial = await Consulta(contexto).ExecuteAsync(new(id, id), default);
            Assert.Null(inicial.Estuda);
            Assert.Null(inicial.Trabalha);
            Assert.Null(inicial.FusoHorario);
            await Fuso(contexto).ExecuteAsync(new(id, id, "America/Sao_Paulo"), default);
            await Edicao(contexto).ExecuteAsync(new(id, id, true, 20.25m, true, 40.5m), default);
        }
        await using (var leitura = banco.NovoContexto())
        {
            var salvo = await Consulta(leitura).ExecuteAsync(new(id, id), default);
            Assert.True(salvo.Estuda);
            Assert.Equal(20.25m, salvo.CargaHorariaEstudoSemanal);
            Assert.True(salvo.Trabalha);
            Assert.Equal(40.5m, salvo.CargaHorariaTrabalhoSemanal);
            Assert.Equal("Pessoa de teste", salvo.Nome);
            Assert.Equal(new DateOnly(1995, 1, 1), salvo.DataNascimento);
            Assert.Equal("America/Sao_Paulo", salvo.FusoHorario);
            await Edicao(leitura).ExecuteAsync(new(id, id, null, null, false, null), default);
        }
        await using var final = banco.NovoContexto();
        var usuario = await final.Usuarios.SingleAsync(u => u.Id == id);
        Assert.Null(usuario.Estuda);
        Assert.Null(usuario.CargaHorariaEstudoSemanal);
        Assert.False(usuario.Trabalha);
        Assert.Null(usuario.CargaHorariaTrabalhoSemanal);
        Assert.Equal(300, usuario.SaldoPontos);
        Assert.Equal("America/Sao_Paulo", usuario.FusoHorario);
        Assert.Equal("hash-apenas-de-teste", usuario.SenhaHash);
    }

    [FatoPostgres]
    public async Task RejeitaAcessoDeOutraContaEContaAusenteSemGravar()
    {
        var id = await banco.CriarUsuarioAsync(10);
        var outro = await banco.CriarUsuarioAsync(20);
        await using var contexto = banco.NovoContexto();
        foreach (var solicitado in new[] { id, Guid.NewGuid() })
        {
            await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Consulta(contexto).ExecuteAsync(new(outro, solicitado), default));
            await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Edicao(contexto).ExecuteAsync(new(outro, solicitado, true, 10, true, 20), default));
            await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Fuso(contexto).ExecuteAsync(new(outro, solicitado, "UTC"), default));
        }
        var ausente = Guid.NewGuid();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Edicao(contexto).ExecuteAsync(new(ausente, ausente, null, null, null, null), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Fuso(contexto).ExecuteAsync(new(ausente, ausente, "UTC"), default));
        Assert.All(await contexto.Usuarios.Where(u => u.Id == id || u.Id == outro).ToListAsync(), u => { Assert.Null(u.Estuda); Assert.Null(u.FusoHorario); });
    }

    [FatoPostgres]
    public async Task FalhaDePersistenciaNaoGravaParteDoPerfil()
    {
        var id = await banco.CriarUsuarioAsync(30);
        await using (var contexto = banco.NovoContexto())
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => Edicao(contexto).ExecuteAsync(new(id, id, false, 20, true, 40), default));
        }
        await using var leitura = banco.NovoContexto();
        var perfil = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Null(perfil.Estuda);
        Assert.Null(perfil.Trabalha);
        Assert.Null(perfil.CargaHorariaTrabalhoSemanal);
    }

    [FatoPostgres]
    public async Task FusoEPerfilConcorrentesPreservamCamposIndependentesERepetirFusoNaoGrava()
    {
        var id = await banco.CriarUsuarioAsync(30);
        await using var perfil = banco.NovoContexto();
        await using var fuso = banco.NovoContexto();
        await perfil.Usuarios.SingleAsync(u => u.Id == id);
        await fuso.Usuarios.SingleAsync(u => u.Id == id);
        await Edicao(perfil).ExecuteAsync(new(id, id, true, 10, false, null), default);
        await Fuso(fuso).ExecuteAsync(new(id, id, "Europe/Lisbon"), default);
        var instante = (await fuso.Usuarios.SingleAsync(u => u.Id == id)).AtualizadoEm;
        await Fuso(fuso).ExecuteAsync(new(id, id, "Europe/Lisbon"), default);
        Assert.Equal(instante, (await fuso.Usuarios.SingleAsync(u => u.Id == id)).AtualizadoEm);
        await using var leitura = banco.NovoContexto();
        var salvo = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal(10, salvo.CargaHorariaEstudoSemanal);
        Assert.Equal("Europe/Lisbon", salvo.FusoHorario);
    }
}
