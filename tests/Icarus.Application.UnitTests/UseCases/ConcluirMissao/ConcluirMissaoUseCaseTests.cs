using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ConcluirMissao;

internal sealed class MissaoRepositorioEmMemoriaParaConcluir : IMissaoRepository
{
    public List<Missao> Missoes { get; } = [];
    public List<Ocorrencia> Ocorrencias { get; set; } = [];

    public Task AdicionarAsync(Missao missao, CancellationToken cancellationToken)
    {
        Missoes.Add(missao);
        return Task.CompletedTask;
    }

    public Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var missao = Missoes.SingleOrDefault(m => m.Id == missaoId && m.UsuarioId == usuarioId);
        if (missao is not null) missao.Ocorrencias = Ocorrencias.Where(o => o.MissaoId == missaoId).ToList();
        return Task.FromResult(missao);
    }

    public Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.UsuarioId == usuarioId && m.CampanhaId == null).ToList());

    public Task<List<Missao>> ListarRecorrentesAtivasAsync(CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.Status == StatusMissao.Ativa && m.Recorrencia is not null).ToList());
}

internal sealed class OcorrenciaRepositorioEmMemoriaParaConcluir : IOcorrenciaRepository
{
    public List<Ocorrencia> Ocorrencias { get; } = [];
    private long _proximoId = 1;

    public Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken)
    {
        ocorrencia.Id = _proximoId++;
        Ocorrencias.Add(ocorrencia);
        return Task.CompletedTask;
    }

    public Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long missaoId, DateOnly dataPrevista, CancellationToken cancellationToken)
        => Task.FromResult(Ocorrencias.SingleOrDefault(o => o.MissaoId == missaoId && o.DataPrevista == dataPrevista));

    public Task<Ocorrencia?> BuscarParaConcluirAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken)
    {
        var deHoje = Ocorrencias.SingleOrDefault(o => o.MissaoId == missaoId && o.DataPrevista == hoje);
        if (deHoje is not null)
        {
            return Task.FromResult<Ocorrencia?>(deHoje);
        }

        var pendente = Ocorrencias
            .Where(o => o.MissaoId == missaoId && (o.Status == StatusOcorrencia.Pendente || o.Status == StatusOcorrencia.Atrasada))
            .OrderBy(o => o.DataPrevista)
            .FirstOrDefault();
        if (pendente is not null)
        {
            return Task.FromResult<Ocorrencia?>(pendente);
        }

        var concluida = Ocorrencias
            .Where(o => o.MissaoId == missaoId && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .OrderByDescending(o => o.DataPrevista)
            .FirstOrDefault();
        return Task.FromResult(concluida);
    }

    public Task<List<DateOnly>> ListarDatasExistentesAsync(long missaoId, DateOnly de, DateOnly ate, CancellationToken cancellationToken)
        => Task.FromResult(Ocorrencias.Where(o => o.MissaoId == missaoId && o.DataPrevista >= de && o.DataPrevista <= ate)
            .Select(o => o.DataPrevista).ToList());

    public Task<List<Ocorrencia>> ListarPendentesNaoVencidasAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken)
        => Task.FromResult(Ocorrencias.Where(o => o.MissaoId == missaoId && o.Status == StatusOcorrencia.Pendente && o.DataPrevista >= hoje).ToList());
}

internal sealed class MovimentacaoPontosRepositorioEmMemoriaParaConcluir : IMovimentacaoPontosRepository
{
    public List<MovimentacaoPontos> Movimentacoes { get; } = [];

    public Task AdicionarAsync(MovimentacaoPontos movimentacao, CancellationToken cancellationToken)
    {
        Movimentacoes.Add(movimentacao);
        return Task.CompletedTask;
    }
}

internal sealed class CampanhaRepositorioEmMemoriaParaConcluirMissao : ICampanhaRepository
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

internal sealed class UsuarioRepositorioEmMemoriaParaConcluirMissao : IUsuarioRepository, IUnitOfWork
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult<Usuario?>(null);

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.SingleOrDefault(u => u.Id == id));

    public Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class ConcluirMissaoUseCaseTests
{
    private readonly MissaoRepositorioEmMemoriaParaConcluir _missaoRepositorio = new();
    private readonly OcorrenciaRepositorioEmMemoriaParaConcluir _ocorrenciaRepositorio = new();
    private readonly MovimentacaoPontosRepositorioEmMemoriaParaConcluir _movimentacaoRepositorio = new();
    private readonly CampanhaRepositorioEmMemoriaParaConcluirMissao _campanhaRepositorio = new();
    private readonly UsuarioRepositorioEmMemoriaParaConcluirMissao _usuarioRepositorio = new();
    private readonly ConcluirMissaoUseCase _useCase;

    public ConcluirMissaoUseCaseTests()
    {
        _missaoRepositorio.Ocorrencias = _ocorrenciaRepositorio.Ocorrencias;
        _useCase = new ConcluirMissaoUseCase(
            _missaoRepositorio, _ocorrenciaRepositorio, _movimentacaoRepositorio, _campanhaRepositorio,
            _usuarioRepositorio, _usuarioRepositorio, new TransacaoConclusaoEmMemoria(), NullLogger<ConcluirMissaoUseCase>.Instance);
    }

    private Usuario AdicionarUsuario(Guid id, long saldoInicial = 0)
    {
        var usuario = new Usuario { Id = id, SaldoPontos = saldoInicial };
        _usuarioRepositorio.Usuarios.Add(usuario);
        return usuario;
    }

    [Fact(DisplayName = "Conclui missão avulsa no prazo: cria ocorrência, credita pontos e soma ao saldo do usuário")]
    public async Task ExecuteAsync_DeveConcluirNoPrazoECreditarPontos()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId, saldoInicial: 100);
        var missao = new Missao { Id = 1, UsuarioId = usuarioId, Titulo = "Beber água", Dificuldade = Dificuldade.Trivial, Pontuacao = 15 };
        _missaoRepositorio.Missoes.Add(missao);

        var resultado = await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.False(resultado.ConcluidaComAtraso);
        Assert.Equal(15, resultado.PontuacaoCreditada);
        Assert.Equal(115, resultado.SaldoPontosAtual);
        Assert.NotNull(missao.ConcluidaEm);

        var ocorrencia = Assert.Single(_ocorrenciaRepositorio.Ocorrencias);
        Assert.Equal(StatusOcorrencia.Concluida, ocorrencia.Status);

        var movimentacao = Assert.Single(_movimentacaoRepositorio.Movimentacoes);
        Assert.Equal(TipoMovimentacaoPontos.GanhoMissao, movimentacao.Tipo);
        Assert.Equal(15, movimentacao.Quantidade);
    }

    [Fact(DisplayName = "Conclui missão com prazo já vencido como concluída com atraso")]
    public async Task ExecuteAsync_DeveMarcarComoAtrasadaQuandoPrazoJaPassou()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId);
        var missao = new Missao
        {
            Id = 1,
            UsuarioId = usuarioId,
            Titulo = "Atrasada",
            Dificuldade = Dificuldade.Facil,
            Pontuacao = 30,
            DataLimite = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5)
        };
        _missaoRepositorio.Missoes.Add(missao);

        var resultado = await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.True(resultado.ConcluidaComAtraso);
        var ocorrencia = Assert.Single(_ocorrenciaRepositorio.Ocorrencias);
        Assert.Equal(StatusOcorrencia.ConcluidaComAtraso, ocorrencia.Status);
    }

    [Fact(DisplayName = "Campanha em pontos mantém o contador de gamificação")]
    public async Task ExecuteAsync_DeveSomarPontosAoValorAtualDaCampanhaMensuravel()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId);
        var campanha = new Campanha { Id = 10, EpicoId = 1, Titulo = "Campanha", Estado = EstadoCampanha.EmAndamento, PrazoInicio = new(2020, 1, 1), PrazoFim = new(2030, 12, 31), ValorMensurado = 100m, ValorAtual = 40m, Unidade = "pontos" };
        _campanhaRepositorio.Campanhas.Add(campanha);
        var missao = new Missao { Id = 1, UsuarioId = usuarioId, CampanhaId = campanha.Id, Titulo = "M", Dificuldade = Dificuldade.Media, Pontuacao = 45 };
        _missaoRepositorio.Missoes.Add(missao);

        await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(85m, campanha.ValorAtual);
    }

    [Theory]
    [InlineData("módulos")]
    [InlineData("reais")]
    [InlineData(null)]
    public async Task ConclusaoNaoDeveSomarPontosAoResultadoMensurado(string? unidade)
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId);
        var campanha = new Campanha { Id = 10, EpicoId = 1, Titulo = "Campanha", Estado = EstadoCampanha.EmAndamento, PrazoInicio = new(2020, 1, 1), PrazoFim = new(2030, 12, 31), ValorMensurado = 10m, ValorAtual = 4m, Unidade = unidade };
        _campanhaRepositorio.Campanhas.Add(campanha);
        _missaoRepositorio.Missoes.Add(new Missao { Id = 1, UsuarioId = usuarioId, CampanhaId = campanha.Id, Titulo = "M", Dificuldade = Dificuldade.Media, Pontuacao = 45 });

        await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, 1), CancellationToken.None);

        Assert.Equal(4m, campanha.ValorAtual);
    }

    [Fact(DisplayName = "Concluir de novo uma missão já concluída é idempotente e não credita pontos outra vez")]
    public async Task ExecuteAsync_DeveSerIdempotenteQuandoJaConcluida()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId, saldoInicial: 0);
        var missao = new Missao { Id = 1, UsuarioId = usuarioId, Titulo = "M", Dificuldade = Dificuldade.Facil, Pontuacao = 30 };
        _missaoRepositorio.Missoes.Add(missao);

        await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);
        var resultado = await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(30, resultado.SaldoPontosAtual); // não dobrou
        Assert.Single(_movimentacaoRepositorio.Movimentacoes);
    }

    [Fact(DisplayName = "Usuário autenticado tentando concluir missão de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ConcluirMissaoCommand(meuId, idDeOutroUsuario, 1), CancellationToken.None));
    }

    [Fact(DisplayName = "Missão inexistente (ou de outro usuário) lança MissaoNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoMissaoNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(
            () => _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, 999), CancellationToken.None));
    }

    [Fact(DisplayName = "Missão inativa não pode ser concluída, mesmo preservando ocorrência atrasada como histórico (RF-04.6)")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoMissaoInativa()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId);
        var missao = new Missao { Id = 1, UsuarioId = usuarioId, Titulo = "Parada", Dificuldade = Dificuldade.Facil, Pontuacao = 30, Status = StatusMissao.Inativa };
        _missaoRepositorio.Missoes.Add(missao);

        await Assert.ThrowsAsync<MissaoInativaException>(
            () => _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None));
    }

    [Fact(DisplayName = "Missão recorrente concluída hoje não marca Missao.ConcluidaEm — ela continua ativa para os próximos dias (DER seção 9)")]
    public async Task ExecuteAsync_NaoDeveMarcarMissaoConcluidaParaRecorrente()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId, saldoInicial: 0);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var missao = new Missao
        {
            Id = 1,
            UsuarioId = usuarioId,
            Titulo = "Beber água",
            Dificuldade = Dificuldade.Trivial,
            Pontuacao = 15,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje.AddDays(-30) }
        };
        _missaoRepositorio.Missoes.Add(missao);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = missao.Id, DataPrevista = hoje });

        var resultado = await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(15, resultado.PontuacaoCreditada);
        Assert.Null(missao.ConcluidaEm);
        Assert.Equal(StatusOcorrencia.Concluida, _ocorrenciaRepositorio.Ocorrencias.Single().Status);
    }

    [Fact(DisplayName = "Concluir de novo a ocorrência de hoje de uma missão recorrente é idempotente")]
    public async Task ExecuteAsync_DeveSerIdempotenteParaOcorrenciaDeHojeDeMissaoRecorrente()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId, saldoInicial: 0);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var missao = new Missao
        {
            Id = 1,
            UsuarioId = usuarioId,
            Titulo = "Beber água",
            Dificuldade = Dificuldade.Trivial,
            Pontuacao = 15,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = hoje.AddDays(-30) }
        };
        _missaoRepositorio.Missoes.Add(missao);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = missao.Id, DataPrevista = hoje });

        await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);
        var resultado = await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.Equal(15, resultado.SaldoPontosAtual); // não dobrou
        Assert.Single(_movimentacaoRepositorio.Movimentacoes);
    }

    [Fact(DisplayName = "Missão recorrente sem ocorrência de hoje, mas com uma pendente de ontem, recupera o dia perdido")]
    public async Task ExecuteAsync_DeveRecuperarOcorrenciaPendenteDeOntemQuandoNaoHaDeHoje()
    {
        var usuarioId = Guid.NewGuid();
        AdicionarUsuario(usuarioId, saldoInicial: 0);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var ontem = hoje.AddDays(-1);
        var missao = new Missao
        {
            Id = 1,
            UsuarioId = usuarioId,
            Titulo = "Beber água",
            Dificuldade = Dificuldade.Trivial,
            Pontuacao = 15,
            Recorrencia = new Recorrencia { MissaoId = 1, Tipo = "DIARIA", DataInicio = ontem }
        };
        _missaoRepositorio.Missoes.Add(missao);
        _ocorrenciaRepositorio.Ocorrencias.Add(new Ocorrencia { Id = 1, MissaoId = missao.Id, DataPrevista = ontem, Status = StatusOcorrencia.Atrasada });

        var resultado = await _useCase.ExecuteAsync(new ConcluirMissaoCommand(usuarioId, usuarioId, missao.Id), CancellationToken.None);

        Assert.True(resultado.ConcluidaComAtraso);
        Assert.Equal(StatusOcorrencia.ConcluidaComAtraso, _ocorrenciaRepositorio.Ocorrencias.Single().Status);
    }
}

internal sealed class TransacaoConclusaoEmMemoria : ITransacaoConclusaoMissao
{
    public Task<T> ExecutarAsync<T>(Guid usuarioId, Func<CancellationToken, Task<T>> operacao, CancellationToken ct)
        => operacao(ct);
}
