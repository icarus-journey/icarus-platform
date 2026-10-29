using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;

namespace Icarus.Application.UnitTests;

internal sealed class IniciadorCampanhasInerte : IIniciadorCampanhas
{
    public Task<int> ExecuteAsync(Guid? usuarioId, CancellationToken ct) => Task.FromResult(0);
}

internal sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}

internal sealed class CicloCampanhaRepositorioEmMemoria : ICicloCampanhaRepository
{
    public List<Missao> Missoes { get; } = [];
    public int Consultas { get; private set; }
    public DateOnly? DataConsultada { get; private set; }
    public Task<List<string?>> ListarFusosPlanejadosAsync(Guid? usuarioId, CancellationToken ct) => Task.FromResult(new List<string?>());
    public Task<int> IniciarPlanejadasAsync(Guid? usuarioId, string? fusoHorario, DateOnly hoje, DateTimeOffset agora, CancellationToken ct) => Task.FromResult(0);
    public Task<List<Missao>> ListarRecorrentesParaEncerrarAsync(long campanhaId, Guid usuarioId, DateOnly hoje, CancellationToken ct)
    {
        Consultas++;
        DataConsultada = hoje;
        return Task.FromResult(Missoes.Where(m => m.CampanhaId == campanhaId && m.UsuarioId == usuarioId && m.Recorrencia != null).ToList());
    }
}
