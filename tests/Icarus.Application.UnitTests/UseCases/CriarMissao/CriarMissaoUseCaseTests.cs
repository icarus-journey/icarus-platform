using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.CriarMissao;

internal sealed class CampanhaRepositorioEmMemoriaParaCriarMissao : ICampanhaRepository
{
    public List<Campanha> Campanhas { get; } = [];

    public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        Campanhas.Add(campanha);
        return Task.CompletedTask;
    }

    public Task<Campanha?> BuscarPorIdAsync(long campanhaId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Campanhas.SingleOrDefault(c => c.Id == campanhaId));
}

internal sealed class MissaoRepositorioEmMemoriaParaCriar : IMissaoRepository, IUnitOfWork
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

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class RecorrenciaRepositorioEmMemoriaParaCriar : IRecorrenciaRepository
{
    public List<Recorrencia> Recorrencias { get; } = [];

    public Task AdicionarAsync(Recorrencia recorrencia, CancellationToken cancellationToken)
    {
        Recorrencias.Add(recorrencia);
        return Task.CompletedTask;
    }
}

internal sealed class OcorrenciaRepositorioEmMemoriaParaCriar : IOcorrenciaRepository
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
}

public class CriarMissaoUseCaseTests
{
    private readonly CampanhaRepositorioEmMemoriaParaCriarMissao _campanhaRepositorio = new();
    private readonly MissaoRepositorioEmMemoriaParaCriar _missaoRepositorio = new();
    private readonly RecorrenciaRepositorioEmMemoriaParaCriar _recorrenciaRepositorio = new();
    private readonly OcorrenciaRepositorioEmMemoriaParaCriar _ocorrenciaRepositorio = new();
    private readonly CriarMissaoUseCase _useCase;

    public CriarMissaoUseCaseTests()
    {
        _useCase = new CriarMissaoUseCase(
            _campanhaRepositorio, _missaoRepositorio, _recorrenciaRepositorio, _ocorrenciaRepositorio,
            _missaoRepositorio, NullLogger<CriarMissaoUseCase>.Instance);
    }

    [Fact(DisplayName = "Cria missão avulsa (sem campanha) calculando a pontuação e usando 09:00 como horário padrão")]
    public async Task ExecuteAsync_DeveCriarMissaoAvulsaComHorarioPadrao()
    {
        var usuarioId = Guid.NewGuid();
        var command = new CriarMissaoCommand(usuarioId, usuarioId, null, "Beber água", null, null, Dificuldade.Trivial, null, null, null);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Null(resultado.CampanhaId);
        Assert.Equal(15, resultado.Pontuacao); // Trivial
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(resultado.Horario.DateTime));
        Assert.False(resultado.Recorrente);
    }

    [Fact(DisplayName = "Cria missão vinculada a uma campanha do usuário, respeitando o horário informado")]
    public async Task ExecuteAsync_DeveCriarMissaoVinculadaACampanha()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha { Id = 1, EpicoId = 1, Titulo = "Campanha", Dificuldade = Dificuldade.Media, Pontuacao = 45 };
        _campanhaRepositorio.Campanhas.Add(campanha);

        var command = new CriarMissaoCommand(
            usuarioId, usuarioId, campanha.Id, "Caminhada", "descrição", "benefício", Dificuldade.Facil, null, "07:30", null);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(campanha.Id, resultado.CampanhaId);
        Assert.Equal(30, resultado.Pontuacao); // Facil
        Assert.Equal(new TimeOnly(7, 30), TimeOnly.FromDateTime(resultado.Horario.DateTime));
    }

    [Fact(DisplayName = "Campanha inexistente (ou de outro usuário) lança CampanhaNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoCampanhaNaoExiste()
    {
        var usuarioId = Guid.NewGuid();
        var command = new CriarMissaoCommand(usuarioId, usuarioId, 999, "Missão", null, null, Dificuldade.Facil, null, null, null);

        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Campanha já concluída não pode receber missão nova — lança CampanhaConcluidaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoCampanhaJaConcluida()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha
        {
            Id = 1, EpicoId = 1, Titulo = "Campanha", Dificuldade = Dificuldade.Media, Pontuacao = 45, Estado = EstadoCampanha.Concluida
        };
        _campanhaRepositorio.Campanhas.Add(campanha);

        var command = new CriarMissaoCommand(usuarioId, usuarioId, campanha.Id, "Missão", null, null, Dificuldade.Facil, null, null, null);

        await Assert.ThrowsAsync<CampanhaConcluidaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando criar missão para outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new CriarMissaoCommand(meuId, idDeOutroUsuario, null, "Missão", null, null, Dificuldade.Facil, null, null, null);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Cria missão não recorrente com exatamente uma ocorrência, na data limite informada")]
    public async Task ExecuteAsync_DeveCriarUmaUnicaOcorrenciaParaMissaoNaoRecorrente()
    {
        var usuarioId = Guid.NewGuid();
        var dataLimite = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        var command = new CriarMissaoCommand(usuarioId, usuarioId, null, "Missão", null, null, Dificuldade.Facil, dataLimite, null, null);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        var ocorrencias = _ocorrenciaRepositorio.Ocorrencias.Where(o => o.MissaoId == resultado.Id).ToList();
        Assert.Single(ocorrencias);
        Assert.Equal(dataLimite, ocorrencias[0].DataPrevista);
        Assert.Empty(_recorrenciaRepositorio.Recorrencias);
    }

    [Fact(DisplayName = "Cria missão diária com 31 ocorrências na janela de 30 dias (hoje + 30)")]
    public async Task ExecuteAsync_DeveGerarJanelaDe31DiasParaMissaoDiaria()
    {
        var usuarioId = Guid.NewGuid();
        var recorrencia = new RecorrenciaCommandInfo(TipoRecorrencia.Diaria, null, null);
        var command = new CriarMissaoCommand(usuarioId, usuarioId, null, "Beber água", null, null, Dificuldade.Trivial, null, null, recorrencia);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.True(resultado.Recorrente);
        var ocorrencias = _ocorrenciaRepositorio.Ocorrencias.Where(o => o.MissaoId == resultado.Id).ToList();
        Assert.Equal(31, ocorrencias.Count);
        Assert.Single(_recorrenciaRepositorio.Recorrencias);
    }

    [Fact(DisplayName = "Cria missão semanal gerando ocorrência só nos dias da semana configurados")]
    public async Task ExecuteAsync_DeveGerarOcorrenciasSoNosDiasDaSemanaConfigurados()
    {
        var usuarioId = Guid.NewGuid();
        var recorrencia = new RecorrenciaCommandInfo(TipoRecorrencia.Semanal, [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday], null);
        var command = new CriarMissaoCommand(usuarioId, usuarioId, null, "Treino", null, null, Dificuldade.Media, null, null, recorrencia);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        var ocorrencias = _ocorrenciaRepositorio.Ocorrencias.Where(o => o.MissaoId == resultado.Id).ToList();
        Assert.All(ocorrencias, o => Assert.Contains(o.DataPrevista.DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }));
        Assert.NotEmpty(ocorrencias);
    }

    [Fact(DisplayName = "DataFim da recorrência, quando anterior à janela de 30 dias, limita a geração de ocorrências e é persistida")]
    public async Task ExecuteAsync_DeveLimitarGeracaoPelaDataFimDaRecorrencia()
    {
        var usuarioId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFim = hoje.AddDays(4);
        var recorrencia = new RecorrenciaCommandInfo(TipoRecorrencia.Diaria, null, dataFim);
        var command = new CriarMissaoCommand(usuarioId, usuarioId, null, "Beber água", null, null, Dificuldade.Trivial, null, null, recorrencia);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        var ocorrencias = _ocorrenciaRepositorio.Ocorrencias.Where(o => o.MissaoId == resultado.Id).ToList();
        Assert.Equal(5, ocorrencias.Count); // hoje + 4 dias
        Assert.All(ocorrencias, o => Assert.True(o.DataPrevista <= dataFim));
        Assert.Equal(dataFim, _recorrenciaRepositorio.Recorrencias.Single().DataFim);
    }
}
