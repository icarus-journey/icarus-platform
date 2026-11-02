using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ExcluirCampanha;

public sealed class ExcluirCampanhaUseCase(IExclusaoPlanejamentoRepository repositorio, IUnitOfWork unidade, TimeProvider relogio)
{
    public async Task ExecuteAsync(ExcluirCampanhaCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var item = await repositorio.BuscarCampanhaAsync(command.CampanhaId, command.UsuarioIdSolicitado, ct)
            ?? throw new CampanhaNaoEncontradaException();
        if (item.Estado == EstadoCampanha.Excluida) return;
        var hoje = FusoHorario.DataLocal(relogio.GetUtcNow(), item.Epico.Usuario.FusoHorario);
        ExclusaoPlanejamento.ExcluirCampanha(item, hoje);
        repositorio.RemoverRascunhos(item);
        await unidade.SalvarAlteracoesAsync(ct);
    }
}
