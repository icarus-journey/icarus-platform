using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarHistoricoRelatorios;
using Icarus.Domain.Entities;
namespace Icarus.Application.UnitTests.HistoricoRelatorios;

public sealed class ConsultaHistoricoRelatoriosTests
{
    private static readonly DateTimeOffset Agora = new(2027, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private sealed class Usuarios : IUsuarioRepository
    {
        public Usuario Pessoa = new() { Id = Guid.NewGuid(), FusoHorario = null };
        public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Usuario?>(id == Pessoa.Id ? Pessoa : null);
        public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> ExisteComEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task AdicionarUsuarioAsync(Usuario u, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Repositorio : IHistoricoRelatoriosRepository
    {
        public DateTimeOffset? Referencia;
        public Task<LoteHistoricoRelatorios> ListarAsync(Guid u, int limite, DateTimeOffset r, CursorHistoricoRelatorios? c, CancellationToken ct) { Referencia = r; return Task.FromResult(new LoteHistoricoRelatorios([], false, 0)); }
        public Task<DetalheRelatorioSalvo?> ConsultarAsync(Guid u, long id, CancellationToken ct) => Task.FromResult<DetalheRelatorioSalvo?>(null);
    }
    private sealed class Cursores : ICursorHistoricoRelatorios
    {
        public CursorHistoricoRelatorios? Valor;
        public bool Lido;
        public string Codificar(CursorHistoricoRelatorios c) => throw new NotSupportedException();
        public CursorHistoricoRelatorios Decodificar(string c) { Lido = true; return Valor!; }
    }
    [Fact]
    public async Task ConsultaContaSemFusoSemInventarPeriodoAtual()
    {
        var u = new Usuarios(); var r = new Repositorio();
        var p = await new ConsultarHistoricoRelatoriosUseCase(r, u, new Cursores(), new Relogio()).ExecuteAsync(new(u.Pessoa.Id, u.Pessoa.Id), default);
        Assert.Empty(p.Relatorios); Assert.Null(p.ProximoCursor); Assert.Equal(Agora, r.Referencia);
    }
    [Fact]
    public async Task IsolaTitularAntesDeLerCursorERecusaDetalheAusente()
    {
        var u = new Usuarios(); var r = new Repositorio(); var c = new Cursores(); var caso = new ConsultarHistoricoRelatoriosUseCase(r, u, c, new Relogio());
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => caso.ExecuteAsync(new(Guid.NewGuid(), u.Pessoa.Id, Cursor: "qualquer"), default));
        Assert.False(c.Lido); Assert.Null(r.Referencia);
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => caso.ConsultarAsync(Guid.NewGuid(), u.Pessoa.Id, 1, default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => caso.ConsultarAsync(u.Pessoa.Id, u.Pessoa.Id, 1, default));
    }
    [Theory]
    [InlineData("dono")]
    [InlineData("tamanho")]
    [InlineData("expirado")]
    [InlineData("futuro")]
    [InlineData("id")]
    [InlineData("ancora")]
    public async Task RejeitaCursorForaDoEscopo(string tipo)
    {
        var u = new Usuarios(); var r = new Repositorio(); var c = new Cursores { Valor = new(u.Pessoa.Id, 20, new(2027, 2, 1), 2, 10, Agora.AddMinutes(-1)) };
        c.Valor = tipo switch { "dono" => c.Valor with { UsuarioId = Guid.NewGuid() }, "tamanho" => c.Valor with { Limite = 10 }, "expirado" => c.Valor with { ConsultadoEm = Agora.AddHours(-25) }, "futuro" => c.Valor with { ConsultadoEm = Agora.AddMinutes(1) }, "id" => c.Valor with { UltimoId = 0 }, _ => c.Valor with { LimiteId = 1 } };
        await Assert.ThrowsAsync<ConsultaHistoricoRelatoriosInvalidaException>(() => new ConsultarHistoricoRelatoriosUseCase(r, u, c, new Relogio()).ExecuteAsync(new(u.Pessoa.Id, u.Pessoa.Id, Cursor: "cursor"), default));
        Assert.Null(r.Referencia);
    }
    [Fact]
    public async Task AncoraTemporalNaoMudaEntrePaginas()
    {
        var u = new Usuarios(); var r = new Repositorio(); var c = new Cursores { Valor = new(u.Pessoa.Id, 20, new(2027, 2, 1), 2, 10, Agora.AddMinutes(-1)) };
        var p = await new ConsultarHistoricoRelatoriosUseCase(r, u, c, new Relogio()).ExecuteAsync(new(u.Pessoa.Id, u.Pessoa.Id, Cursor: "cursor"), default);
        Assert.Equal(c.Valor.ConsultadoEm, p.ConsultadoEm); Assert.Equal(c.Valor.ConsultadoEm, r.Referencia);
    }
}
