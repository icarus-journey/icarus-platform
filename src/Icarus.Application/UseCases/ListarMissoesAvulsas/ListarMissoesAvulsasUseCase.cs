using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ListarMissoesAvulsas;

/// <summary>
/// Caso de uso de listagem das missões avulsas do usuário (sem campanha, RF-03). Missões
/// atreladas a uma campanha já voltam em <c>GET /usuarios/{id}/epicos</c>, aninhadas no épico e
/// na campanha — este endpoint cobre só o que fica de fora daquele grafo.
/// </summary>
public sealed partial class ListarMissoesAvulsasUseCase(IMissaoRepository missaoRepository, ILogger<ListarMissoesAvulsasUseCase> logger)
{
    public async Task<List<MissaoAvulsaListadaResult>> ExecuteAsync(ListarMissoesAvulsasCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var missoes = await missaoRepository.ListarAvulsasAsync(command.UsuarioIdSolicitado, cancellationToken);

        return missoes
            .Select(missao => new MissaoAvulsaListadaResult(
                missao.Id, missao.Titulo, missao.Descricao, missao.Beneficio, missao.Dificuldade,
                missao.Pontuacao, missao.DataLimite, missao.Horario, missao.Status))
            .ToList();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de listar missões avulsas de outro usuário rejeitada")]
    private partial void LogAcessoNegado();
}
