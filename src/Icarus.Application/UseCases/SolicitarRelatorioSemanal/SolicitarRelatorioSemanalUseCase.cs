using System.Globalization;
using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarRelatorioSemanal;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.SolicitarRelatorioSemanal;

public sealed partial class SolicitarRelatorioSemanalUseCase(IRelatorioSemanalRepository repositorio,
    IIndicadoresPessoaisRepository indicadores, IUnitOfWork unidade, TimeProvider relogio,
    ILogger<SolicitarRelatorioSemanalUseCase> logger)
{
    public async Task<ConsultarRelatorioSemanalResult> ExecuteAsync(SolicitarRelatorioSemanalCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        ConsultarRelatorioSemanalResult? resultado = null;
        await repositorio.ExecutarAsync(command.UsuarioIdSolicitado, async (usuario, token) =>
        {
            if (!Icarus.Application.Common.FusoHorario.EhValido(usuario.FusoHorario))
            {
                resultado = new("fuso_nao_informado", null, "Abra novamente o aplicativo para sincronizar o fuso e consultar sua semana."); return;
            }
            var agora = DateTimeOffset.FromUnixTimeMilliseconds(relogio.GetUtcNow().ToUnixTimeMilliseconds());
            var periodo = PeriodoAnalitico.Encerrado("semanaAnterior", usuario.FusoHorario!, agora);
            var relatorio = await repositorio.BuscarRelatorioAsync(usuario.Id, periodo.Inicio, periodo.Fim, token);
            var trabalho = await repositorio.BuscarTrabalhoAsync(usuario.Id, periodo.Inicio, periodo.Fim, token);
            if (relatorio is not null || (trabalho is not null &&
                ((trabalho.Estado == "processando" && trabalho.ExpiraEm > agora) || !command.NovaTentativa || trabalho.Tentativas >= ContratoRelatorioSemanal.MaxTentativas)))
            {
                resultado = ApresentacaoRelatorioSemanal.Criar(periodo, relatorio, trabalho, agora); return;
            }
            var dados = await indicadores.CalcularAsync(usuario.Id, periodo, token);
            if (dados.QuantidadeRegistros == 0)
            {
                resultado = new("historico_insuficiente", periodo,
                    "Ainda não há registros suficientes dessa semana para preparar uma síntese pessoal. As referências públicas continuam disponíveis."); return;
            }
            if (trabalho is null)
            {
                trabalho = new() { Id = Guid.NewGuid(), UsuarioId = usuario.Id, PeriodoInicio = periodo.Inicio, PeriodoFim = periodo.Fim, FusoHorario = periodo.FusoHorario };
                await repositorio.AdicionarTrabalhoAsync(trabalho, token);
            }
            var anterior = trabalho.TentativaId == Guid.Empty ? (Guid?)null : trabalho.TentativaId;
            trabalho.TentativaId = Guid.NewGuid(); trabalho.Tentativas++; trabalho.Estado = "processando";
            trabalho.ExpiraEm = agora.AddMinutes(5); trabalho.FalhaCodigo = null;
            trabalho.FusoHorario = periodo.FusoHorario;
            trabalho.Indicadores = JsonSerializer.Serialize(dados, ContratoRelatorioSemanal.Json);
            var mensagem = new SolicitacaoRelatorioSemanal(trabalho.TentativaId, trabalho.Id, anterior,
                "relatorio.semanal.solicitar", ContratoRelatorioSemanal.Versao, agora, trabalho.ExpiraEm,
                trabalho.Id.ToString("N") + ":" + trabalho.Tentativas.ToString(CultureInfo.InvariantCulture),
                periodo, IndicadoresRelatorioSemanal.DosIndicadores(dados));
            await repositorio.AdicionarOutboxAsync(new() { Id = mensagem.IdMensagem, TrabalhoId = trabalho.Id,
                Carga = JsonSerializer.Serialize(mensagem, ContratoRelatorioSemanal.Json), ProximaTentativaEm = agora }, token);
            await unidade.SalvarAlteracoesAsync(token);
            LogSolicitado();
            resultado = ApresentacaoRelatorioSemanal.Criar(periodo, null, trabalho, agora);
        }, ct);
        return resultado!;
    }
    [LoggerMessage(Level = LogLevel.Information, Message = "Geração de relatório semanal registrada na outbox")]
    private partial void LogSolicitado();
}
