using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ListarEntradasDiario;

public sealed partial class ListarEntradasDiarioUseCase(
    IDiarioRepository diario, IUsuarioRepository usuarios)
{
    public async Task<List<EntradaDiarioResult>> ExecuteAsync(ListarEntradasDiarioCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado
            || await usuarios.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken) is null)
            throw new PerfilNaoEncontradoException();
        var entradas = await diario.ListarAsync(command.UsuarioIdSolicitado, cancellationToken);
        return entradas.Select(EntradaDiarioResult.DaEntrada).ToList();
    }
}
