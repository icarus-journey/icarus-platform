using Icarus.Application.Common;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.IniciarCampanhas;

/// <summary>RF-03.7: recupera também datas iniciais que passaram enquanto a API estava parada.</summary>
public sealed class IniciarCampanhasUseCase(ICicloCampanhaRepository campanhas, TimeProvider relogio) : IIniciadorCampanhas
{
    public async Task<int> ExecuteAsync(Guid? usuarioId, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var total = 0;
        foreach (var fuso in await campanhas.ListarFusosPlanejadosAsync(usuarioId, ct))
            total += await campanhas.IniciarPlanejadasAsync(usuarioId, fuso, FusoHorario.DataLocal(agora, fuso), agora, ct);
        return total;
    }
}
