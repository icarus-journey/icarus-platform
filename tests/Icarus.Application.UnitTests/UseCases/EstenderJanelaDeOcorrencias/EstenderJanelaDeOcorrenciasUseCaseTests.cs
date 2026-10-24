using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.EstenderJanelaDeOcorrencias;

internal sealed class MissaoRepositorioEmMemoriaParaEstenderJanela : IMissaoRepository
{
    public List<Missao> Missoes { get; } = [];

    public Task AdicionarAsync(Missao missao, CancellationToken cancellationToken)
    {
        Missoes.Add(missao);
        return Task.CompletedTask;
    }

    public Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.SingleOrDefault(m => m.Id == missaoId));

    public Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.UsuarioId == usuarioId && m.CampanhaId == null).ToList());

    public Task<List<Missao>> ListarRecorrentesAtivasAsync(CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.Status == StatusMissao.Ativa && m.Recorrencia is not null).ToList());
}

internal sealed class OcorrenciaRepositorioEmMemoriaParaEstenderJanela : IOcorrenciaRepository, IUnitOfWork
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

public class EstenderJanelaDeOcorrenciasUseCaseTests
{
    private readonly MissaoRepositorioEmMemoriaParaEstenderJanela _missaoRepositorio = new();
    private readonly OcorrenciaRepositorioEmMemoriaParaEstenderJanela _ocorrenciaRepositorio = new();
    private readonly EstenderJanelaDeOcorrenciasUseCase _useCase;

    public EstenderJanelaDeOcorrenciasUseCaseTests()
    {
        _useCase = new EstenderJanelaDeOcorrenciasUseCase(
            _missaoRepositorio, _ocorrenciaRepositorio, _ocorrenciaRepositorio, NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance);
    }

    [Fact(DisplayName = "Missão diária ativa sem ocorrências ganha a janela completa dos próximos 30 dias")]
    public async Task ExecuteAsync_DeveGerarJanelaCompletaQuandoNaoHaOcorrenciaAlguma()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Beber água", Status = StatusMissao.Ativa,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje }
        };
        _missaoRepositorio.Missoes.Add(missao);

        var resultado = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, resultado.MissoesProcessadas);
        Assert.Equal(31, resultado.OcorrenciasCriadas); // hoje + 30 dias
    }

    [Fact(DisplayName = "Não duplica ocorrências que já existem na janela")]
    public async Task ExecuteAsync_NaoDeveDuplicarOcorrenciasExistentes()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Beber água", Status = StatusMissao.Ativa,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje }
        };
        _missaoRepositorio.Missoes.Add(missao);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { MissaoId = 1, DataPrevista = hoje });

        var resultado = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(30, resultado.OcorrenciasCriadas); // só as 30 que faltavam
        Assert.Equal(31, _ocorrenciaRepositorio.Ocorrencias.Count(o => o.MissaoId == 1));
    }

    [Fact(DisplayName = "Recorrência com data fim anterior à janela não gera ocorrência além dela")]
    public async Task ExecuteAsync_DeveRespeitarDataFimDaRecorrencia()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFim = hoje.AddDays(3);
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Beber água", Status = StatusMissao.Ativa,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje, DataFim = dataFim }
        };
        _missaoRepositorio.Missoes.Add(missao);

        var resultado = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(4, resultado.OcorrenciasCriadas); // hoje, +1, +2, +3
        Assert.All(_ocorrenciaRepositorio.Ocorrencias, o => Assert.True(o.DataPrevista <= dataFim));
    }

    [Fact(DisplayName = "Missão inativa ou sem recorrência é ignorada")]
    public async Task ExecuteAsync_DeveIgnorarMissaoInativaOuSemRecorrencia()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _missaoRepositorio.Missoes.Add(new Missao { Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Sem recorrência", Status = StatusMissao.Ativa });
        _missaoRepositorio.Missoes.Add(new Missao
        {
            Id = 2, UsuarioId = Guid.NewGuid(), Titulo = "Inativa", Status = StatusMissao.Inativa,
            Recorrencia = new Recorrencia { MissaoId = 2, Tipo = "DIARIA", DataInicio = hoje }
        });

        var resultado = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, resultado.MissoesProcessadas);
        Assert.Equal(0, resultado.OcorrenciasCriadas);
    }

    [Fact(DisplayName = "Inativa automaticamente a missão cuja data fim da recorrência já passou (RF-04.6)")]
    public async Task ExecuteAsync_DeveInativarAutomaticamenteQuandoDataFimJaPassou()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFimVencida = hoje.AddDays(-1);
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Beber água", Status = StatusMissao.Ativa,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje.AddDays(-10), DataFim = dataFimVencida }
        };
        _missaoRepositorio.Missoes.Add(missao);

        var resultado = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, resultado.MissoesInativadasPorFimDaRecorrencia);
        Assert.Equal(0, resultado.OcorrenciasCriadas);
        Assert.Equal(StatusMissao.Inativa, missao.Status);
    }

    [Fact(DisplayName = "Inativação automática cancela pendente não vencida remanescente, mas preserva atrasada como histórico")]
    public async Task ExecuteAsync_InativacaoAutomaticaCancelaPendenteNaoVencidaEPreservaAtrasada()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFimVencida = hoje.AddDays(-1);
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Beber água", Status = StatusMissao.Ativa,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje.AddDays(-10), DataFim = dataFimVencida }
        };
        _missaoRepositorio.Missoes.Add(missao);
        // Defensivo: não deveria existir ocorrência futura à data fim, mas cobre o caso.
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = 1, DataPrevista = hoje.AddDays(1), Status = StatusOcorrencia.Pendente });
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 2, MissaoId = 1, DataPrevista = dataFimVencida, Status = StatusOcorrencia.Pendente });

        await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StatusOcorrencia.Excluida, _ocorrenciaRepositorio.Ocorrencias.Single(o => o.Id == 1).Status);
        Assert.Equal(StatusOcorrencia.Pendente, _ocorrenciaRepositorio.Ocorrencias.Single(o => o.Id == 2).Status);
    }

    [Fact(DisplayName = "Data fim igual a hoje ainda não é considerada vencida — continua gerando normalmente")]
    public async Task ExecuteAsync_NaoDeveInativarQuandoDataFimEHoje()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Beber água", Status = StatusMissao.Ativa,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje, DataFim = hoje }
        };
        _missaoRepositorio.Missoes.Add(missao);

        var resultado = await _useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, resultado.MissoesInativadasPorFimDaRecorrencia);
        Assert.Equal(StatusMissao.Ativa, missao.Status);
    }
}
