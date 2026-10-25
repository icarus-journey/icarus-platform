using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ConsultarProgressoCampanha;

public sealed class ConsultarProgressoCampanhaUseCase(ICampanhaRepository campanhas, IMedicaoCampanhaRepository medicoes, TimeProvider relogio, IIniciadorCampanhas iniciador)
{
    public async Task<ProgressoCampanhaResult> ExecuteAsync(ConsultarProgressoCampanhaCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new CampanhaNaoEncontradaException();
        await iniciador.ExecuteAsync(command.UsuarioIdSolicitado, ct);
        var campanha = await campanhas.BuscarPorIdAsync(command.CampanhaId, command.UsuarioIdSolicitado, ct)
            ?? throw new CampanhaNaoEncontradaException();
        if (campanha.Estado == EstadoCampanha.Excluida) throw new CampanhaNaoEncontradaException();
        return ProgressoCampanhaResult.DaCampanha(campanha, await medicoes.ListarAsync(campanha.Id, command.UsuarioIdSolicitado, ct), relogio.GetUtcNow());
    }
}
