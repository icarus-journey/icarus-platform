using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirCampanha;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ConcluirCampanha;

internal sealed class CampanhaRepositorioEmMemoriaParaConcluir : ICampanhaRepository, IUnitOfWork
{
    public List<Campanha> Campanhas { get; } = [];

    public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        Campanhas.Add(campanha);
        return Task.CompletedTask;
    }

    public Task<Campanha?> BuscarPorIdAsync(long campanhaId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Campanhas.SingleOrDefault(c => c.Id == campanhaId));

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class ConcluirCampanhaUseCaseTests
{
    private readonly CampanhaRepositorioEmMemoriaParaConcluir _repositorio = new();
    private readonly ConcluirCampanhaUseCase _useCase;

    public ConcluirCampanhaUseCaseTests()
    {
        _useCase = new ConcluirCampanhaUseCase(_repositorio, _repositorio, NullLogger<ConcluirCampanhaUseCase>.Instance, new CicloCampanhaRepositorioEmMemoria(), TimeProvider.System);
    }

    [Fact(DisplayName = "Conclui a campanha mesmo sem o valor mensurado atingido (confirmação manual, RF-03.7)")]
    public async Task ExecuteAsync_DeveConcluirIndependenteDoValorAtual()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha { Id = 1, EpicoId = 1, Titulo = "C", ValorMensurado = 100m, ValorAtual = 10m, Estado = EstadoCampanha.EmAndamento };
        _repositorio.Campanhas.Add(campanha);

        var resultado = await _useCase.ExecuteAsync(new ConcluirCampanhaCommand(usuarioId, usuarioId, campanha.Id), CancellationToken.None);

        Assert.Equal(EstadoCampanha.Concluida, resultado.Estado);
        Assert.Equal(EstadoCampanha.Concluida, campanha.Estado);
    }

    [Fact(DisplayName = "Concluir de novo uma campanha já concluída é idempotente")]
    public async Task ExecuteAsync_DeveSerIdempotente()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha { Id = 1, EpicoId = 1, Titulo = "C", Estado = EstadoCampanha.Concluida };
        _repositorio.Campanhas.Add(campanha);

        var resultado = await _useCase.ExecuteAsync(new ConcluirCampanhaCommand(usuarioId, usuarioId, campanha.Id), CancellationToken.None);

        Assert.Equal(EstadoCampanha.Concluida, resultado.Estado);
    }

    [Fact(DisplayName = "Usuário autenticado tentando concluir campanha de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ConcluirCampanhaCommand(meuId, idDeOutroUsuario, 1), CancellationToken.None));
    }

    [Fact(DisplayName = "Campanha inexistente (ou de outro usuário) lança CampanhaNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoCampanhaNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(
            () => _useCase.ExecuteAsync(new ConcluirCampanhaCommand(usuarioId, usuarioId, 999), CancellationToken.None));
    }
    [Fact]
    public async Task CancelaSomentePlanejamentoElegivelNoDiaLocalEPreservaConclusoes()
    {
        var usuario = Guid.NewGuid();
        var campanha = new Campanha
        {
            Id = 1,
            Estado = EstadoCampanha.EmAndamento,
            Epico = new Epico { Usuario = new Usuario { FusoHorario = "America/Sao_Paulo" } }
        };
        _repositorio.Campanhas.Add(campanha);
        var ciclo = new CicloCampanhaRepositorioEmMemoria();
        var hoje = new DateOnly(2026, 10, 24);
        var ocorrencias = new[] {
            new Ocorrencia { DataPrevista = hoje.AddDays(-1) },
            new Ocorrencia { DataPrevista = hoje },
            new Ocorrencia { DataPrevista = hoje.AddDays(1) },
            new Ocorrencia { DataPrevista = hoje.AddDays(2), Status = StatusOcorrencia.Concluida },
            new Ocorrencia { DataPrevista = hoje, Status = StatusOcorrencia.ConcluidaComAtraso }
        };
        var missao = new Missao { UsuarioId = usuario, CampanhaId = 1, Recorrencia = new(), Ocorrencias = ocorrencias.ToList() };
        ciclo.Missoes.Add(missao);
        var caso = new ConcluirCampanhaUseCase(_repositorio, _repositorio, NullLogger<ConcluirCampanhaUseCase>.Instance,
            ciclo, new RelogioFixo(new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero)));
        await caso.ExecuteAsync(new(usuario, usuario, 1), default);
        Assert.Equal(hoje, ciclo.DataConsultada);
        Assert.Equal(StatusMissao.Inativa, missao.Status);
        Assert.Equal(new[] { StatusOcorrencia.Pendente, StatusOcorrencia.Excluida, StatusOcorrencia.Excluida,
            StatusOcorrencia.Concluida, StatusOcorrencia.ConcluidaComAtraso }, ocorrencias.Select(o => o.Status));
        await caso.ExecuteAsync(new(usuario, usuario, 1), default);
        Assert.Equal(1, ciclo.Consultas);
    }

    [Fact]
    public async Task ExcluidaNaoPodeSerConcluida()
    {
        _repositorio.Campanhas.Add(new Campanha { Id = 1, Estado = EstadoCampanha.Excluida });
        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => _useCase.ExecuteAsync(new(Guid.Empty, Guid.Empty, 1), default));
        Assert.Equal(EstadoCampanha.Excluida, _repositorio.Campanhas[0].Estado);
    }

}
