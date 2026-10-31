using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarMissao;

/// <summary>Edita campos planejados sem alterar vínculo, execução registrada ou pontos anteriores.</summary>
public sealed partial class AtualizarMissaoUseCase(
    IPlanejamentoMissaoRepository repositorio, IUnitOfWork unidade,
    ILogger<AtualizarMissaoUseCase> logger, TimeProvider relogio)
{
    public async Task<EdicaoMissaoResult> ExecuteAsync(AtualizarMissaoCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var missao = await repositorio.BuscarAsync(command.MissaoId, command.UsuarioIdSolicitado, ct) ?? throw new MissaoNaoEncontradaException();
        if (missao.Status == StatusMissao.Excluida) throw new MissaoNaoEncontradaException();
        if (missao.Status != StatusMissao.Ativa) throw new MissaoInativaException();
        if (missao.Recorrencia is null && (missao.ConcluidaEm is not null || missao.Ocorrencias.Any(Concluida)))
            throw new MissaoConcluidaException();
        if (missao.Campanha?.Estado == EstadoCampanha.Concluida) throw new CampanhaConcluidaException();
        if (missao.Campanha?.Estado == EstadoCampanha.Excluida) throw new CampanhaNaoEncontradaException();

        var hoje = FusoHorario.DataLocal(relogio.GetUtcNow(), missao.Usuario?.FusoHorario);
        if (missao.Recorrencia is not null)
        {
            ValidarRecorrencia(command, missao, hoje);
            PlanejamentoOcorrencia.RegistrarVersaoAnterior(missao, hoje);
            var anterior = PlanejamentoOcorrencia.Capturar(missao);
            foreach (var ocorrencia in missao.Ocorrencias.Where(o => o.DataPrevista > hoje && !Concluida(o) && o.Planejamento is null))
                ocorrencia.Planejamento = anterior;
        }
        else if (command.Recorrencia is not null)
            throw new EdicaoMissaoException("tipo_de_missao_imutavel", "Crie outra missão para mudar entre única e recorrente.", true);

        missao.Titulo = command.Titulo;
        missao.Descricao = command.Descricao;
        missao.Beneficio = command.Beneficio;
        missao.Dificuldade = command.Dificuldade;
        missao.Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade);
        missao.DataLimite = command.DataLimite;
        var hora = command.Horario is not null && TimeOnly.TryParse(command.Horario, out var parsed) ? parsed : new TimeOnly(9, 0);
        missao.Horario = new DateTimeOffset((command.DataLimite ?? hoje).ToDateTime(hora), TimeSpan.Zero);

        if (missao.Recorrencia is { } recorrencia)
        {
            var diaDoMes = command.DiaDoMes ?? command.DataLimite?.Day ?? ConfiguracaoRecorrenciaResult.DaMissao(missao)?.DiaDoMes ?? hoje.AddDays(1).Day;
            recorrencia.Tipo = command.Recorrencia!.Value.ToString().ToUpperInvariant();
            recorrencia.Regra = GeradorDeDatasRecorrencia.ConstruirRegra(command.Recorrencia.Value, command.DiasSemana, diaDoMes);
            recorrencia.DataFim = command.DataFimRecorrencia;
            recorrencia.AlteracoesAPartirDe = hoje.AddDays(1);
            Replanejar(missao, hoje);
        }

        missao.AtualizadoEm = relogio.GetUtcNow();
        if (missao.Campanha is { } campanha) campanha.AtualizadoEm = relogio.GetUtcNow();
        await unidade.SalvarAlteracoesAsync(ct);
        LogMissaoAtualizada(missao.Id);
        return EdicaoMissaoResult.DaMissao(missao);
    }

    private static void ValidarRecorrencia(AtualizarMissaoCommand c, Missao m, DateOnly hoje)
    {
        if (c.Recorrencia is null || !Enum.IsDefined(c.Recorrencia.Value))
            throw new EdicaoMissaoException("recorrencia_obrigatoria", "Informe como a missão deve se repetir.");
        if (c.VersaoEsperada != EdicaoMissaoResult.VersaoDaMissao(m))
            throw new DadosAtualizadosException(new InvalidOperationException("Versão da missão divergente."));
        if (c.Recorrencia == TipoRecorrencia.Semanal && (c.DiasSemana is not { Count: > 0 } || c.DiasSemana.Any(d => !Enum.IsDefined(d))))
            throw new EdicaoMissaoException("dias_invalidos", "Escolha ao menos um dia da semana válido.");
        if (c.DiaDoMes is < 1 or > 31) throw new EdicaoMissaoException("dia_invalido", "Escolha um dia entre 1 e 31.");
        if (c.DataFimRecorrencia < hoje.AddDays(1))
            throw new EdicaoMissaoException("fim_invalido", "A repetição deve terminar a partir de amanhã. Para encerrar agora, use Parar missão.");
    }

    private static bool Concluida(Ocorrencia o) => o.ConcluidaEm is not null
        || o.Status is StatusOcorrencia.Concluida or StatusOcorrencia.ConcluidaComAtraso;

    private static void Replanejar(Missao missao, DateOnly hoje)
    {
        var r = missao.Recorrencia!;
        var inicio = r.DataInicio > hoje.AddDays(1) ? r.DataInicio : hoje.AddDays(1);
        var fim = hoje.AddDays(GeradorDeDatasRecorrencia.DiasDaJanelaPadrao);
        if (r.DataFim is { } dataFim && dataFim < fim) fim = dataFim;
        if (missao.Campanha is { } campanha)
        {
            if (campanha.PrazoInicio > inicio) inicio = campanha.PrazoInicio;
            if (campanha.PrazoFim < fim) fim = campanha.PrazoFim;
        }
        var datas = GeradorDeDatasRecorrencia.Gerar(inicio, fim, Enum.Parse<TipoRecorrencia>(r.Tipo, true), r.Regra).ToHashSet();
        var planejamento = PlanejamentoOcorrencia.Capturar(missao);
        foreach (var ocorrencia in missao.Ocorrencias.Where(o => o.DataPrevista > hoje && !Concluida(o)))
        {
            if (datas.Contains(ocorrencia.DataPrevista))
            {
                ocorrencia.Status = StatusOcorrencia.Pendente;
                ocorrencia.Planejamento = planejamento;
            }
            else ocorrencia.Status = StatusOcorrencia.Excluida;
        }
        var existentes = missao.Ocorrencias.Select(o => o.DataPrevista).ToHashSet();
        foreach (var data in datas.Except(existentes))
            missao.Ocorrencias.Add(new Ocorrencia { Missao = missao, DataPrevista = data, Planejamento = planejamento });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Planejamento da missão {MissaoId} atualizado")]
    private partial void LogMissaoAtualizada(long missaoId);
}
