using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.RelatoSono;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class RelatoSonoPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static RelatoSonoUseCase Caso(IcarusDbContext contexto) => new(new UsuarioRepository(contexto), contexto, TimeProvider.System);

    [FatoPostgres]
    public async Task SalvaRemoveEPreservaPerfilSaldoEFoto()
    {
        var id = await banco.CriarUsuarioAsync(150);
        await using var contexto = banco.NovoContexto();
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Id == id);
        usuario.FotoChave = "foto-preservada";
        await contexto.SaveChangesAsync();
        var caso = Caso(contexto);
        var vazio = await caso.ConsultarAsync(id, id, default);
        Assert.Null(vazio.HabitualMinutos);
        var salvo = await caso.AtualizarAsync(new(id, id, 360, 420, vazio.Versao), default);
        Assert.Equal(60, salvo.DiferencaParaMetaMinutos);
        Assert.NotNull(salvo.InformadoEm);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => caso.AtualizarAsync(new(id, id, 400, 420, vazio.Versao), default));
        var removido = await caso.AtualizarAsync(new(id, id, null, null, salvo.Versao), default);
        Assert.Null(removido.HabitualMinutos);
        Assert.Null(removido.MetaMinutos);
        Assert.Null(removido.InformadoEm);
        Assert.Equal(150, usuario.SaldoPontos);
        Assert.Equal("foto-preservada", usuario.FotoChave);
        Assert.Equal("Pessoa de teste", usuario.Nome);
    }

    [FatoPostgres]
    public async Task IsolaTitularERejeitaEdicaoConcorrenteComContextoAntigo()
    {
        var id = await banco.CriarUsuarioAsync(0);
        await using var primeiro = banco.NovoContexto();
        await using var segundo = banco.NovoContexto();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Caso(primeiro).ConsultarAsync(Guid.NewGuid(), id, default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Caso(primeiro).AtualizarAsync(new(Guid.NewGuid(), id, 360, null, Guid.Empty), default));
        var versao = (await Caso(segundo).ConsultarAsync(id, id, default)).Versao;
        await Caso(primeiro).AtualizarAsync(new(id, id, 360, null, versao), default);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Caso(segundo).AtualizarAsync(new(id, id, 400, null, versao), default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(360, (await Caso(leitura).ConsultarAsync(id, id, default)).HabitualMinutos);
    }
}
