using Icarus.Application.Common.Jornada;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarJornada;

namespace Icarus.Application.UnitTests.Analise;

public sealed class ConsultarJornadaTests
{
    private sealed class Repositorio : IJornadaRepository
    {
        public int Consultas { get; private set; }
        public DadosJornada? Dados { get; set; } = new(1270, "UTC", []);
        public Task<DadosJornada?> BuscarAsync(Guid usuarioId, CancellationToken ct)
        {
            Consultas++;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Dados);
        }
    }

    [Fact]
    public async Task TitularRecebeSaldoRealEContratoVersionado()
    {
        var r = new Repositorio(); var id = Guid.NewGuid();
        var resultado = await new ConsultarJornadaUseCase(r, TimeProvider.System).ExecuteAsync(new(id, id), default);
        Assert.Equal(1270, resultado.SaldoPontos);
        Assert.Equal("1.0.0", resultado.VersaoContrato);
        Assert.Equal("sem_registros", resultado.Sequencia.Estado);
    }

    [Fact]
    public async Task ContaAlheiaNaoChegaAoRepositorio()
    {
        var r = new Repositorio();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => new ConsultarJornadaUseCase(r, TimeProvider.System).ExecuteAsync(new(Guid.NewGuid(), Guid.NewGuid()), default));
        Assert.Equal(0, r.Consultas);
    }

    [Fact]
    public async Task ContaAusenteRetornaNaoEncontrado()
    {
        var r = new Repositorio { Dados = null }; var id = Guid.NewGuid();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => new ConsultarJornadaUseCase(r, TimeProvider.System).ExecuteAsync(new(id, id), default));
    }

    [Fact]
    public async Task SaldoContinuaDisponivelQuandoFaltaFuso()
    {
        var r = new Repositorio { Dados = new(450, null, []) }; var id = Guid.NewGuid();
        var resultado = await new ConsultarJornadaUseCase(r, TimeProvider.System).ExecuteAsync(new(id, id), default);
        Assert.Equal(450, resultado.SaldoPontos);
        Assert.Null(resultado.Sequencia.DiasAtuais);
    }
}
