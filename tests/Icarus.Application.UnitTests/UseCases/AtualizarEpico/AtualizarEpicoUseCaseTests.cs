using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarEpico;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.AtualizarEpico;

internal sealed class EpicoRepositorioEmMemoriaParaAtualizar : IEpicoRepository, IUnitOfWork
{
    public List<Epico> Epicos { get; } = [];

    public Task AdicionarAsync(Epico epico, CancellationToken cancellationToken)
    {
        Epicos.Add(epico);
        return Task.CompletedTask;
    }

    public Task<Epico?> BuscarComCampanhaEMissoesAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.SingleOrDefault(e => e.Id == epicoId && e.UsuarioId == usuarioId));

    public Task<Epico?> BuscarPorIdAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.SingleOrDefault(e => e.Id == epicoId && e.UsuarioId == usuarioId));

    public Task<List<Epico>> ListarComCampanhasEMissoesAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.Where(e => e.UsuarioId == usuarioId).ToList());

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class AtualizarEpicoUseCaseTests
{
    private readonly EpicoRepositorioEmMemoriaParaAtualizar _repositorio = new();
    private readonly AtualizarEpicoUseCase _useCase;

    public AtualizarEpicoUseCaseTests()
    {
        _useCase = new AtualizarEpicoUseCase(_repositorio, _repositorio, NullLogger<AtualizarEpicoUseCase>.Instance);
    }

    [Fact(DisplayName = "Atualiza título, descrição, dificuldade e prazos, recalculando a pontuação, sem alterar a área da vida")]
    public async Task ExecuteAsync_DeveAtualizarRecalculandoPontuacaoSemAlterarAreaDaVida()
    {
        var usuarioId = Guid.NewGuid();
        var epico = new Epico
        {
            Id = 1,
            UsuarioId = usuarioId,
            Titulo = "Antigo",
            AreaDaVida = AreaDaVida.CorpoESaude,
            Dificuldade = Dificuldade.Trivial,
            Pontuacao = 15,
            PrazoInicio = new DateOnly(2026, 1, 1),
            PrazoFim = new DateOnly(2026, 12, 31)
        };
        _repositorio.Epicos.Add(epico);

        var command = new AtualizarEpicoCommand(
            usuarioId, usuarioId, epico.Id, "Novo título", "nova descrição", Dificuldade.Dificil,
            new DateOnly(2026, 2, 1), new DateOnly(2027, 2, 1));

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal("Novo título", resultado.Titulo);
        Assert.Equal(60, resultado.Pontuacao); // Dificil
        Assert.Equal(AreaDaVida.CorpoESaude, resultado.AreaDaVida); // inalterada
        Assert.Equal(new DateOnly(2027, 2, 1), resultado.PrazoFim);
    }

    [Fact(DisplayName = "Épico já concluído é imutável — edição lança EpicoConcluidoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoEpicoJaConcluido()
    {
        var usuarioId = Guid.NewGuid();
        var epico = new Epico
        {
            Id = 1, UsuarioId = usuarioId, Titulo = "Concluído", AreaDaVida = AreaDaVida.CorpoESaude, Dificuldade = Dificuldade.Facil,
            Pontuacao = 30, PrazoInicio = new DateOnly(2026, 1, 1), PrazoFim = new DateOnly(2026, 12, 31), Estado = EstadoEpico.Concluido
        };
        _repositorio.Epicos.Add(epico);

        var command = new AtualizarEpicoCommand(
            usuarioId, usuarioId, epico.Id, "Novo título", null, Dificuldade.Media, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        await Assert.ThrowsAsync<EpicoConcluidoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
        Assert.Equal("Concluído", epico.Titulo); // não foi alterado
    }

    [Fact(DisplayName = "Usuário autenticado tentando editar épico de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new AtualizarEpicoCommand(
            meuId, idDeOutroUsuario, 1, "Título", null, Dificuldade.Facil, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1));

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Épico inexistente (ou de outro usuário) lança EpicoNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoEpicoNaoExiste()
    {
        var usuarioId = Guid.NewGuid();
        var command = new AtualizarEpicoCommand(
            usuarioId, usuarioId, 999, "Título", null, Dificuldade.Facil, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1));

        await Assert.ThrowsAsync<EpicoNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }
}
