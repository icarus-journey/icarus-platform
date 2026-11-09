using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class PontuacaoManualPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    [FatoPostgres]
    public async Task CriacaoEdicaoEConclusaoUsamValorEscolhidoSemRecalcularOuCreditarDuasVezes()
    {
        var usuario = await banco.CriarUsuarioAsync(500);
        long id;
        await using (var c = banco.NovoContexto())
        {
            (await c.Usuarios.SingleAsync(u => u.Id == usuario)).FusoHorario = "UTC";
            await c.SaveChangesAsync();
            var criar = new CriarMissaoUseCase(new CampanhaRepository(c), new MissaoRepository(c),
                new RecorrenciaRepository(c), new OcorrenciaRepository(c), c,
                NullLogger<CriarMissaoUseCase>.Instance, TimeProvider.System, new UsuarioRepository(c));
            var criada = await criar.ExecuteAsync(new(usuario, usuario, null, "Revisar uma aula", null, null,
                Dificuldade.Trivial, 137, null, null, null), default);
            id = criada.Id;
            Assert.Equal(137, criada.Pontuacao);
        }
        await using (var c = banco.NovoContexto())
        {
            var editar = new AtualizarMissaoUseCase(new PlanejamentoMissaoRepository(c), c,
                NullLogger<AtualizarMissaoUseCase>.Instance, TimeProvider.System);
            var editada = await editar.ExecuteAsync(new(usuario, usuario, id, "Revisar uma aula", null, null,
                Dificuldade.Dificil, 1_000_000, null, null), default);
            Assert.Equal(1_000_000, editada.Pontuacao);
            Assert.Equal(500, (await c.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
            Assert.Empty(await c.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());
        }
        await using (var c = banco.NovoContexto())
        {
            var concluir = new ConcluirMissaoUseCase(new MissaoRepository(c), new OcorrenciaRepository(c),
                new MovimentacaoPontosRepository(c), new CampanhaRepository(c), new UsuarioRepository(c),
                c, new TransacaoConclusaoMissao(c), NullLogger<ConcluirMissaoUseCase>.Instance);
            var concluida = await concluir.ExecuteAsync(new(usuario, usuario, id), default);
            var repetida = await concluir.ExecuteAsync(new(usuario, usuario, id), default);
            Assert.Equal(1_000_000, concluida.PontuacaoCreditada);
            Assert.Equal(1_000_000, repetida.PontuacaoCreditada);
            Assert.Equal(1_000_500, repetida.SaldoPontosAtual);
            Assert.Equal(1_000_000, (await c.MovimentacoesPontos.SingleAsync(m => m.UsuarioId == usuario)).Quantidade);
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal(1_000_000, (await leitura.Missoes.SingleAsync(m => m.Id == id)).Pontuacao);
        Assert.Equal(1_000_500, (await leitura.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
        Assert.Single(await leitura.Ocorrencias.Where(o => o.MissaoId == id && o.ConcluidaEm != null).ToListAsync());
    }
}
