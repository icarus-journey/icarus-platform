using Icarus.Application.Common;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.PersistirResultadoOnboarding;

/// <summary>
/// Caso de uso que persiste o resultado assíncrono do worker de IA — chamado pelo consumer
/// RabbitMQ, nunca pela API diretamente. Calcula a pontuação de Épico/Campanha/Missão a
/// partir da dificuldade (nunca confia em pontuação vinda do worker) e é idempotente: um
/// job_id já concluído, ou inexistente, é ignorado (reentrega da fila).
/// </summary>
public sealed partial class PersistirResultadoOnboardingUseCase(
    ITrabalhoOnboardingRepository trabalhoRepository,
    IEpicoRepository epicoRepository,
    IUnitOfWork unitOfWork,
    ILogger<PersistirResultadoOnboardingUseCase> logger)
{
    public async Task ExecuteAsync(PersistirResultadoOnboardingCommand command, CancellationToken cancellationToken)
    {
        var trabalho = await trabalhoRepository.BuscarPorIdAsync(command.JobId, cancellationToken);
        if (trabalho is null)
        {
            LogTrabalhoNaoEncontrado(command.JobId);
            return;
        }

        if (trabalho.Status != StatusTrabalhoOnboarding.Pendente)
        {
            LogResultadoDuplicado(command.JobId);
            return;
        }

        var epicoPrazoFim = command.Epico.PrazoFim ?? command.Epico.PrazoInicio.AddYears(1);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var missoes = command.Missoes.Select(m => new Missao
        {
            UsuarioId = trabalho.UsuarioId,
            Titulo = m.Titulo,
            Descricao = m.Descricao,
            Beneficio = m.Beneficio,
            Dificuldade = m.Dificuldade,
            Pontuacao = MapeadorPontuacao.ParaPontuacao(m.Dificuldade),
            DataLimite = m.DataLimite,
            Horario = ParaHorario(hoje, m.Horario)
        }).ToList();

        var campanha = new Campanha
        {
            Titulo = command.Campanha.Titulo,
            Descricao = command.Campanha.Descricao,
            AreaDaVida = command.Campanha.AreaDaVida,
            Dificuldade = command.Campanha.Dificuldade,
            Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Campanha.Dificuldade),
            PrazoInicio = command.Campanha.PrazoInicio,
            PrazoFim = command.Campanha.PrazoFim ?? epicoPrazoFim,
            // DER seção 8: pontuação total não é duplicada — soma das missões, calculada aqui
            // porque o worker não sabe mais pontuação de nada.
            ValorMensurado = missoes.Sum(m => (decimal)m.Pontuacao),
            Unidade = "pontos",
            ValorAtual = 0,
            Missoes = missoes
        };

        var epico = new Epico
        {
            UsuarioId = trabalho.UsuarioId,
            Titulo = command.Epico.Titulo,
            Descricao = command.Epico.Descricao,
            AreaDaVida = command.Epico.AreaDaVida,
            Dificuldade = command.Epico.Dificuldade,
            Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Epico.Dificuldade),
            PrazoInicio = command.Epico.PrazoInicio,
            PrazoFim = epicoPrazoFim,
            Campanhas = [campanha]
        };

        await epicoRepository.AdicionarAsync(epico, cancellationToken);

        trabalho.Status = StatusTrabalhoOnboarding.Concluido;
        // Seta a navegação, não o Id escalar: o Id do épico só existe depois do insert, e o EF
        // resolve a FK de TrabalhoOnboarding automaticamente dentro do mesmo SaveChanges.
        trabalho.Epico = epico;

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogResultadoPersistido(command.JobId, epico.Id);
    }

    private static DateTimeOffset ParaHorario(DateOnly data, string? horario)
    {
        var hora = horario is not null && TimeOnly.TryParse(horario, out var parsed) ? parsed : new TimeOnly(9, 0);
        return new DateTimeOffset(data.ToDateTime(hora), TimeSpan.Zero);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resultado de onboarding recebido para trabalho {JobId} inexistente — ignorado")]
    private partial void LogTrabalhoNaoEncontrado(Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resultado de onboarding {JobId} já processado — reentrega ignorada")]
    private partial void LogResultadoDuplicado(Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Onboarding {JobId} persistido: épico {EpicoId} criado")]
    private partial void LogResultadoPersistido(Guid jobId, long epicoId);
}
