using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.CriarEpico;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.CriarEpico;

internal sealed class EpicoRepositorioEmMemoriaParaCriar : IEpicoRepository, IUnitOfWork
{
    public List<Epico> Epicos { get; } = [];
    private long _proximoId = 1;

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

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        foreach (var epico in Epicos.Where(e => e.Id == 0))
        {
            epico.Id = _proximoId++;
        }

        return Task.CompletedTask;
    }
}

public class CriarEpicoUseCaseTests
{
    private readonly EpicoRepositorioEmMemoriaParaCriar _repositorio = new();
    private readonly CriarEpicoUseCase _useCase;

    public CriarEpicoUseCaseTests()
    {
        _useCase = new CriarEpicoUseCase(_repositorio, _repositorio, NullLogger<CriarEpicoUseCase>.Instance);
    }

    [Fact(DisplayName = "Cria épico calculando a pontuação a partir da dificuldade, com prazos padrão quando omitidos")]
    public async Task ExecuteAsync_DeveCriarComPontuacaoCalculadaEPrazosPadrao()
    {
        var usuarioId = Guid.NewGuid();
        var command = new CriarEpicoCommand(
            usuarioId, usuarioId, "Ser promovido", "descrição", AreaDaVida.TrabalhoECarreira, Dificuldade.Dificil, null, null);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(60, resultado.Pontuacao); // Dificil
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), resultado.PrazoInicio);
        Assert.Equal(resultado.PrazoInicio.AddYears(1), resultado.PrazoFim);
        Assert.Equal(EstadoEpico.EmAndamento, resultado.Estado);

        var epico = Assert.Single(_repositorio.Epicos);
        Assert.Equal(usuarioId, epico.UsuarioId);
    }

    [Fact(DisplayName = "Prazo fim informado é respeitado em vez do padrão")]
    public async Task ExecuteAsync_DeveRespeitarPrazoFimInformado()
    {
        var usuarioId = Guid.NewGuid();
        var prazoFim = new DateOnly(2027, 3, 1);
        var command = new CriarEpicoCommand(
            usuarioId, usuarioId, "Épico", null, AreaDaVida.Financas, Dificuldade.Facil, new DateOnly(2026, 1, 1), prazoFim);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(prazoFim, resultado.PrazoFim);
    }

    [Fact(DisplayName = "Usuário autenticado tentando criar épico para outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new CriarEpicoCommand(
            meuId, idDeOutroUsuario, "Épico", null, AreaDaVida.Financas, Dificuldade.Facil, null, null);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
        Assert.Empty(_repositorio.Epicos);
    }
}
