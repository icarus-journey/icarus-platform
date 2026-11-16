using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.InativarMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.InativarMissao;

internal sealed class MissaoRepositorioEmMemoriaParaInativar : IMissaoRepository
{
    public List<Missao> Missoes { get; } = [];

    public Task AdicionarAsync(Missao missao, CancellationToken cancellationToken)
    {
        Missoes.Add(missao);
        return Task.CompletedTask;
    }

    public Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.SingleOrDefault(m => m.Id == missaoId && m.UsuarioId == usuarioId));

    public Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.UsuarioId == usuarioId && m.CampanhaId == null).ToList());

    public Task<List<Missao>> ListarRecorrentesAtivasAsync(CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.Status == StatusMissao.Ativa && m.Recorrencia is not null).ToList());
}

internal sealed class OcorrenciaRepositorioEmMemoriaParaInativar : IOcorrenciaRepository, IUnitOfWork
{
    public List<Ocorrencia> Ocorrencias { get; } = [];

    public Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken)
    {
        Ocorrencias.Add(ocorrencia);
        return Task.CompletedTask;
    }

    public Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long missaoId, DateOnly dataPrevista, CancellationToken cancellationToken)
        => Task.FromResult(Ocorrencias.SingleOrDefault(o => o.MissaoId == missaoId && o.DataPrevista == dataPrevista));

    public Task<Ocorrencia?> BuscarParaConcluirAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken)
        => Task.FromResult<Ocorrencia?>(null);

    public Task<List<DateOnly>> ListarDatasExistentesAsync(long missaoId, DateOnly de, DateOnly ate, CancellationToken cancellationToken)
        => Task.FromResult(Ocorrencias.Where(o => o.MissaoId == missaoId && o.DataPrevista >= de && o.DataPrevista <= ate)
            .Select(o => o.DataPrevista).ToList());

    public Task<List<Ocorrencia>> ListarPendentesNaoVencidasAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken)
        => Task.FromResult(Ocorrencias.Where(o => o.MissaoId == missaoId && o.Status == StatusOcorrencia.Pendente && o.DataPrevista >= hoje).ToList());

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class InativarMissaoUseCaseTests
{
    private readonly MissaoRepositorioEmMemoriaParaInativar _missaoRepositorio = new();
    private readonly OcorrenciaRepositorioEmMemoriaParaInativar _ocorrenciaRepositorio = new();
    private readonly InativarMissaoUseCase _useCase;

    public InativarMissaoUseCaseTests()
    {
        _useCase = new InativarMissaoUseCase(
            _missaoRepositorio, _ocorrenciaRepositorio, _ocorrenciaRepositorio, NullLogger<InativarMissaoUseCase>.Instance);
    }

    private static Missao MissaoRecorrente(Guid usuarioId, long id = 1, StatusMissao status = StatusMissao.Ativa) => new()
    {
        Id = id, UsuarioId = usuarioId, Titulo = "Beber água", Status = status,
        Recorrencia = new Recorrencia { MissaoId = id, Tipo = "DIARIA", DataInicio = DateOnly.FromDateTime(DateTime.UtcNow) }
    };

    [Fact(DisplayName = "Inativa missão recorrente, cancelando as ocorrências pendentes ainda não vencidas")]
    public async Task ExecuteAsync_DeveInativarECancelarPendentesNaoVencidas()
    {
        var usuarioId = Guid.NewGuid();
        var missao = MissaoRecorrente(usuarioId);
        _missaoRepositorio.Missoes.Add(missao);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = missao.Id, DataPrevista = hoje });
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 2, MissaoId = missao.Id, DataPrevista = hoje.AddDays(5) });

        var resultado = await _useCase.ExecuteAsync(new InativarMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(StatusMissao.Inativa, resultado.Status);
        Assert.Equal(2, resultado.OcorrenciasCanceladas);
        Assert.Equal(StatusMissao.Inativa, missao.Status);
        Assert.All(_ocorrenciaRepositorio.Ocorrencias, o => Assert.Equal(StatusOcorrencia.Excluida, o.Status));
    }

    [Fact(DisplayName = "Preserva ocorrência pendente já vencida (sem marca de Atrasada) como histórico, sem cancelá-la")]
    public async Task ExecuteAsync_NaoDeveCancelarPendenteJaVencida()
    {
        var usuarioId = Guid.NewGuid();
        var missao = MissaoRecorrente(usuarioId);
        _missaoRepositorio.Missoes.Add(missao);
        var ontem = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = missao.Id, DataPrevista = ontem, Status = StatusOcorrencia.Pendente });

        var resultado = await _useCase.ExecuteAsync(new InativarMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(0, resultado.OcorrenciasCanceladas);
        Assert.Equal(StatusOcorrencia.Pendente, _ocorrenciaRepositorio.Ocorrencias.Single().Status);
    }

    [Fact(DisplayName = "Missão não recorrente não pode ser inativada (RF-04.6)")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoMissaoNaoRecorrente()
    {
        var usuarioId = Guid.NewGuid();
        var missao = new Missao { Id = 1, UsuarioId = usuarioId, Titulo = "Avulsa", Status = StatusMissao.Ativa };
        _missaoRepositorio.Missoes.Add(missao);

        await Assert.ThrowsAsync<MissaoNaoRecorrenteException>(
            () => _useCase.ExecuteAsync(new InativarMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None));
    }

    [Fact(DisplayName = "Inativar de novo uma missão já inativa é idempotente e não cancela nada outra vez")]
    public async Task ExecuteAsync_DeveSerIdempotenteQuandoJaInativa()
    {
        var usuarioId = Guid.NewGuid();
        var missao = MissaoRecorrente(usuarioId, status: StatusMissao.Inativa);
        _missaoRepositorio.Missoes.Add(missao);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = missao.Id, DataPrevista = DateOnly.FromDateTime(DateTime.UtcNow) });

        var resultado = await _useCase.ExecuteAsync(new InativarMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(0, resultado.OcorrenciasCanceladas);
        Assert.Equal(StatusOcorrencia.Pendente, _ocorrenciaRepositorio.Ocorrencias.Single().Status); // não tocou
    }

    [Fact(DisplayName = "Missão inexistente (ou de outro usuário) lança MissaoNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoMissaoNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(
            () => _useCase.ExecuteAsync(new InativarMissaoCommand(usuarioId, usuarioId, 999), CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando inativar missão de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new InativarMissaoCommand(meuId, idDeOutroUsuario, 1), CancellationToken.None));
    }
}
