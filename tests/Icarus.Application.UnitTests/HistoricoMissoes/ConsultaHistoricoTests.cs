using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using Icarus.Domain.Entities;
namespace Icarus.Application.UnitTests.HistoricoMissoes;

public sealed class ConsultaHistoricoTests
{
    private static readonly DateTimeOffset Agora = new(2027, 1, 20, 2, 0, 0, TimeSpan.Zero);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private sealed class Usuarios : IUsuarioRepository
    {
        public Usuario Pessoa = new() { Id = Guid.NewGuid(), FusoHorario = "America/Sao_Paulo" };
        public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Usuario?>(id == Pessoa.Id ? Pessoa : null);
        public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> ExisteComEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task AdicionarUsuarioAsync(Usuario u, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Repositorio : IHistoricoMissoesRepository
    {
        public FiltroHistoricoMissoes? Recebido; public DateTimeOffset? Referencia;
        public Task<LoteHistoricoMissoes> ListarAsync(Guid u, FiltroHistoricoMissoes f, DateTimeOffset r, string z, CursorHistoricoMissoes? c, CancellationToken ct) { Recebido = f; Referencia = r; return Task.FromResult(new LoteHistoricoMissoes([], false, 0, false)); }
        public Task<RegistroHistoricoMissao?> ConsultarAsync(Guid u, long id, DateOnly hoje, CancellationToken ct) => Task.FromResult<RegistroHistoricoMissao?>(null);
        public Task<IReadOnlyList<CampanhaHistoricoMissao>> ListarCampanhasAsync(Guid u, CancellationToken ct) => Task.FromResult<IReadOnlyList<CampanhaHistoricoMissao>>([]);
    }
    private sealed class Cursores : ICursorHistoricoMissoes
    {
        public CursorHistoricoMissoes? Valor;
        public string Codificar(CursorHistoricoMissoes c) => throw new NotSupportedException();
        public CursorHistoricoMissoes Decodificar(string c) => Valor!;
    }
    [Fact]
    public async Task PadraoUsaTrintaDiasCivisDoTitular()
    {
        var u = new Usuarios(); var r = new Repositorio(); var caso = new ConsultarHistoricoMissoesUseCase(r, u, new Cursores(), new Relogio());
        var p = await caso.ExecuteAsync(new(u.Pessoa.Id, u.Pessoa.Id), default);
        Assert.Equal(new DateOnly(2026, 12, 21), p.Filtro.Inicio); Assert.Equal(new DateOnly(2027, 1, 19), p.Filtro.Fim); Assert.Equal("America/Sao_Paulo", p.FusoHorario); Assert.False(p.TemRegistros);
    }
    [Fact]
    public async Task OutroTitularFalhaAntesDeLerCursorOuConsultarDados()
    {
        var u = new Usuarios(); var r = new Repositorio(); var caso = new ConsultarHistoricoMissoesUseCase(r, u, new Cursores(), new Relogio());
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => caso.ExecuteAsync(new(Guid.NewGuid(), u.Pessoa.Id, Cursor: "qualquer"), default)); Assert.Null(r.Recebido);
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => caso.ConsultarAsync(u.Pessoa.Id, u.Pessoa.Id, 999, default));
    }
    [Theory]
    [InlineData("titular")]
    [InlineData("filtros")]
    [InlineData("fuso")]
    [InlineData("expirado")]
    [InlineData("futuro")]
    [InlineData("limite")]
    public async Task CursorNaoPodeMudarSeuEscopo(string mudanca)
    {
        var u = new Usuarios(); var r = new Repositorio(); var c = new Cursores { Valor = new(u.Pessoa.Id, new(new(2027, 1, 1), new(2027, 1, 31), EstadoHistoricoMissao.Todos, null, false, 20), new(2027, 1, 19), 10, 20, Agora.AddMinutes(-1), u.Pessoa.FusoHorario!) };
        c.Valor = mudanca switch { "titular" => c.Valor with { UsuarioId = Guid.NewGuid() }, "filtros" => c.Valor with { Filtro = c.Valor.Filtro with { Estado = EstadoHistoricoMissao.Feita } }, "fuso" => c.Valor with { FusoHorario = "UTC" }, "expirado" => c.Valor with { ConsultadoEm = Agora.AddDays(-2) }, "futuro" => c.Valor with { ConsultadoEm = Agora.AddMinutes(1) }, _ => c.Valor with { LimiteId = 1 } };
        var caso = new ConsultarHistoricoMissoesUseCase(r, u, c, new Relogio());
        await Assert.ThrowsAsync<ConsultaHistoricoInvalidaException>(() => caso.ExecuteAsync(new(u.Pessoa.Id, u.Pessoa.Id, Cursor: "cursor"), default)); Assert.Null(r.Recebido);
    }
    [Fact]
    public async Task ProximaPaginaMantemReferenciaTemporalDoCursor()
    {
        var u = new Usuarios(); var r = new Repositorio(); var c = new Cursores { Valor = new(u.Pessoa.Id, new(new(2027, 1, 1), new(2027, 1, 31), EstadoHistoricoMissao.Todos, null, false, 20), new(2027, 1, 19), 10, 20, Agora.AddMinutes(-5), u.Pessoa.FusoHorario!) };
        var p = await new ConsultarHistoricoMissoesUseCase(r, u, c, new Relogio()).ExecuteAsync(new(u.Pessoa.Id, u.Pessoa.Id, Cursor: "cursor"), default);
        Assert.Equal(c.Valor.ConsultadoEm, p.ConsultadoEm); Assert.Equal(c.Valor.Filtro, p.Filtro); Assert.Equal(c.Valor.ConsultadoEm, r.Referencia);
    }
}
