using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ExcluirEpico;

public sealed class ExcluirEpicoUseCase(IExclusaoPlanejamentoRepository repositorio, IUnitOfWork unidade, TimeProvider relogio)
{
    public async Task ExecuteAsync(ExcluirEpicoCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var item = await repositorio.BuscarEpicoAsync(command.EpicoId, command.UsuarioIdSolicitado, ct)
            ?? throw new EpicoNaoEncontradoException();
        if (item.Estado == EstadoEpico.Excluido) return;
        var hoje = FusoHorario.DataLocal(relogio.GetUtcNow(), item.Usuario.FusoHorario);
        foreach (var campanha in item.Campanhas)
        {
            ExclusaoPlanejamento.ExcluirCampanha(campanha, hoje);
            repositorio.RemoverRascunhos(campanha);
            campanha.AtualizadoEm = relogio.GetUtcNow();
        }
        item.Estado = EstadoEpico.Excluido;
        await unidade.SalvarAlteracoesAsync(ct);
    }
}
