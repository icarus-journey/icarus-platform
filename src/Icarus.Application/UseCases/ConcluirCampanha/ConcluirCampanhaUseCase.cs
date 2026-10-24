using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ConcluirCampanha;

/// <summary>
/// Caso de uso de conclusão manual de campanha (RF-03.7). Atingir o valor mensurado não
/// conclui a campanha por si só — esta é a confirmação explícita do usuário. Idempotente:
/// concluir de novo uma campanha já concluída é um no-op. Cancelamento de ocorrências futuras
/// (RF-03.7) não se aplica ainda: geração de ocorrências futuras (RF-04.5) não existe no sistema.
/// </summary>
public sealed partial class ConcluirCampanhaUseCase(
    ICampanhaRepository campanhaRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConcluirCampanhaUseCase> logger)
{
    public async Task<ConcluirCampanhaResult> ExecuteAsync(ConcluirCampanhaCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var campanha = await campanhaRepository.BuscarPorIdAsync(command.CampanhaId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new CampanhaNaoEncontradaException();

        if (campanha.Estado != EstadoCampanha.Concluida)
        {
            campanha.Estado = EstadoCampanha.Concluida;
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
            LogCampanhaConcluida(campanha.Id);
        }

        return new ConcluirCampanhaResult(campanha.Id, campanha.Estado);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de concluir campanha de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Campanha {CampanhaId} concluída")]
    private partial void LogCampanhaConcluida(long campanhaId);
}
