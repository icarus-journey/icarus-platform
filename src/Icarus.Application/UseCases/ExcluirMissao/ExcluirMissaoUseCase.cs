using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ExcluirMissao;

public sealed class ExcluirMissaoUseCase(IExclusaoPlanejamentoRepository repositorio, IUnitOfWork unidade, TimeProvider relogio)
{
    public async Task ExecuteAsync(ExcluirMissaoCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var item = await repositorio.BuscarMissaoAsync(command.MissaoId, command.UsuarioIdSolicitado, ct)
            ?? throw new MissaoNaoEncontradaException();
        if (item.Status == StatusMissao.Excluida) return;
        var hoje = FusoHorario.DataLocal(relogio.GetUtcNow(), item.Usuario.FusoHorario);
        if (!ExclusaoPlanejamento.ExcluirMissao(item, hoje)) throw new MissaoConcluidaException();
        await unidade.SalvarAlteracoesAsync(ct);
    }
}
