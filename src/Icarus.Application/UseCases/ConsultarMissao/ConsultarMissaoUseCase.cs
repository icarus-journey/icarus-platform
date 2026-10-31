using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarMissao;

public sealed record ConsultarMissaoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long MissaoId);

public sealed class ConsultarMissaoUseCase(IPlanejamentoMissaoRepository repositorio, IUnitOfWork unidade, TimeProvider relogio, IIniciadorCampanhas? iniciador = null)
{
    public async Task<EdicaoMissaoResult> ExecuteAsync(ConsultarMissaoCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new MissaoNaoEncontradaException();
        if (iniciador is not null) await iniciador.ExecuteAsync(command.UsuarioIdSolicitado, ct);
        var missao = await repositorio.BuscarAsync(command.MissaoId, command.UsuarioIdSolicitado, ct) ?? throw new MissaoNaoEncontradaException();
        var agora = relogio.GetUtcNow();
        if (RegrasRecorrencia.EncerrarSeNecessario(missao, FusoHorario.DataLocal(agora, missao.Usuario?.FusoHorario)))
            await unidade.SalvarAlteracoesAsync(ct);
        return EdicaoMissaoResult.DaMissao(missao, agora);
    }
}
