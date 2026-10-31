using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarMissao;

public sealed record ConsultarMissaoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long MissaoId);

public sealed class ConsultarMissaoUseCase(IPlanejamentoMissaoRepository repositorio)
{
    public async Task<EdicaoMissaoResult> ExecuteAsync(ConsultarMissaoCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new MissaoNaoEncontradaException();
        var missao = await repositorio.BuscarAsync(command.MissaoId, command.UsuarioIdSolicitado, ct) ?? throw new MissaoNaoEncontradaException();
        return EdicaoMissaoResult.DaMissao(missao);
    }
}
