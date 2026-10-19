using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ListarEpicos;

/// <summary>Caso de uso de listagem dos épicos do usuário, com campanhas e missões (RF-01.5).</summary>
public sealed partial class ListarEpicosUseCase(IEpicoRepository epicoRepository, ILogger<ListarEpicosUseCase> logger)
{
    public async Task<List<EpicoListadoResult>> ExecuteAsync(ListarEpicosCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var epicos = await epicoRepository.ListarComCampanhasEMissoesAsync(command.UsuarioIdSolicitado, cancellationToken);

        return epicos.Select(epico => new EpicoListadoResult(
            epico.Id, epico.Titulo, epico.Descricao, epico.AreaDaVida, epico.Dificuldade, epico.Pontuacao,
            epico.PrazoInicio, epico.PrazoFim, epico.Estado,
            epico.Campanhas.Select(campanha => new CampanhaListadaResult(
                campanha.Id, campanha.Titulo, campanha.Descricao, campanha.AreaDaVida, campanha.Dificuldade,
                campanha.Pontuacao, campanha.PrazoInicio, campanha.PrazoFim, campanha.ValorMensurado,
                campanha.Unidade, campanha.ValorAtual, campanha.Estado,
                campanha.Missoes.Select(missao => new MissaoListadaResult(
                    missao.Id, missao.Titulo, missao.Descricao, missao.Beneficio, missao.Dificuldade,
                    missao.Pontuacao, missao.DataLimite, missao.Horario, missao.Status, missao.ConcluidaEm))
                    .ToList()))
                .ToList()))
            .ToList();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de listar épicos de outro usuário rejeitada")]
    private partial void LogAcessoNegado();
}
