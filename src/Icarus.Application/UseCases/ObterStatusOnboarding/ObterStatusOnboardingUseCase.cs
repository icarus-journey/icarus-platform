using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ObterStatusOnboarding;

/// <summary>Caso de uso de consultar o status (e, quando concluído, o resultado) de um onboarding.</summary>
public sealed partial class ObterStatusOnboardingUseCase(
    ITrabalhoOnboardingRepository trabalhoRepository,
    IEpicoRepository epicoRepository,
    ILogger<ObterStatusOnboardingUseCase> logger)
{
    public async Task<ObterStatusOnboardingResult> ExecuteAsync(ObterStatusOnboardingCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var trabalho = await trabalhoRepository.BuscarPorIdAsync(command.JobId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new TrabalhoOnboardingNaoEncontradoException();

        if (trabalho.Status == StatusTrabalhoOnboarding.Falhou)
        {
            return new ObterStatusOnboardingResult(trabalho.Status, null, trabalho.ErroMensagem);
        }

        if (trabalho.Status == StatusTrabalhoOnboarding.Pendente || trabalho.EpicoId is null)
        {
            return new ObterStatusOnboardingResult(trabalho.Status, null, null);
        }

        var epico = await epicoRepository.BuscarComCampanhaEMissoesAsync(trabalho.EpicoId.Value, trabalho.UsuarioId, cancellationToken)
            ?? throw new TrabalhoOnboardingNaoEncontradoException();

        var campanha = epico.Campanhas.Single();

        var missoes = campanha.Missoes
            .Select(m => new MissaoOnboardingResult(m.Id, m.Titulo, m.Descricao, m.Beneficio, m.Dificuldade, m.Pontuacao, m.DataLimite, m.Horario, m.Status))
            .ToList();

        var campanhaResult = new CampanhaOnboardingResult(
            campanha.Id, campanha.Titulo, campanha.Descricao, campanha.AreaDaVida, campanha.Dificuldade, campanha.Pontuacao,
            campanha.PrazoInicio, campanha.PrazoFim, campanha.ValorMensurado, campanha.Unidade, campanha.ValorAtual, campanha.Estado, missoes);

        var epicoResult = new EpicoOnboardingResult(
            epico.Id, epico.Titulo, epico.Descricao, epico.AreaDaVida, epico.Dificuldade, epico.Pontuacao,
            epico.PrazoInicio, epico.PrazoFim, epico.Estado, campanhaResult);

        return new ObterStatusOnboardingResult(trabalho.Status, epicoResult, null);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de consultar onboarding de outro usuário rejeitada")]
    private partial void LogAcessoNegado();
}
