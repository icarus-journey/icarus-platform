using System.Globalization;
using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.PersistirRelatorioSemanal;

public sealed partial class PersistirRelatorioSemanalUseCase(IRelatorioSemanalRepository repositorio, IUnitOfWork unidade,
    TimeProvider relogio, ILogger<PersistirRelatorioSemanalUseCase> logger)
{
    public async Task ExecuteAsync(ResultadoRelatorioSemanal mensagem, CancellationToken ct)
    {
        var titular = await repositorio.BuscarTitularAsync(mensagem.CorrelacaoId, ct);
        if (titular is null) { LogIgnorado(); return; }
        await repositorio.ExecutarAsync(titular.Value, async (_, token) =>
        {
            if (await repositorio.ResultadoRecebidoAsync(mensagem.IdMensagem, token)) return;
            var trabalho = (await repositorio.BuscarPorIdAsync(mensagem.CorrelacaoId, token))!;
            var agora = relogio.GetUtcNow();
            var chave = trabalho.Id.ToString("N") + ":" + trabalho.Tentativas.ToString(CultureInfo.InvariantCulture);
            var periodoValido = mensagem.Periodo is not null && mensagem.Periodo.Inicio == trabalho.PeriodoInicio
                && mensagem.Periodo.Fim == trabalho.PeriodoFim && mensagem.Periodo.FusoHorario == trabalho.FusoHorario;
            var vigente = trabalho.TentativaId == mensagem.CausalidadeId && trabalho.Estado == "processando"
                && trabalho.ExpiraEm > agora && mensagem.ExpiraEm == trabalho.ExpiraEm
                && mensagem.ChaveIdempotencia == chave && periodoValido;
            var inbox = new InboxRelatorio { Id = mensagem.IdMensagem, TrabalhoId = trabalho.Id,
                TentativaId = mensagem.CausalidadeId, Estado = "ignorada" };
            await repositorio.AdicionarInboxAsync(inbox, token);
            if (vigente)
            {
                var valido = mensagem.Tipo == "relatorio.semanal.resultado" && mensagem.VersaoContrato == ContratoRelatorioSemanal.Versao
                    && mensagem.Estado == "concluido" && mensagem.FalhaCodigo is null
                    && ContratoRelatorioSemanal.ConteudoValido(mensagem.Conteudo)
                    && mensagem.Geracao is { Provedor: "openai" or "ollama", Modelo.Length: > 0 and <= 120 };
                if (valido)
                {
                    var existente = await repositorio.BuscarRelatorioAsync(trabalho.UsuarioId, trabalho.PeriodoInicio, trabalho.PeriodoFim, token);
                    if (existente is null)
                    {
                        var dados = JsonSerializer.Deserialize<IndicadoresPessoais>(trabalho.Indicadores, ContratoRelatorioSemanal.Json)!;
                        await repositorio.AdicionarRelatorioAsync(new() { UsuarioId = trabalho.UsuarioId, Tipo = TipoRelatorio.Semanal,
                            PeriodoInicio = trabalho.PeriodoInicio, PeriodoFim = trabalho.PeriodoFim, Texto = mensagem.Conteudo!.Resumo,
                            Conteudo = JsonSerializer.Serialize(new SnapshotRelatorioSemanal(mensagem.Conteudo, dados, mensagem.Geracao!), ContratoRelatorioSemanal.Json) }, token);
                    }
                    trabalho.Estado = "concluido"; inbox.Estado = "aplicada";
                }
                else
                {
                    trabalho.Estado = "falha"; trabalho.FalhaCodigo = mensagem.Estado == "falha" ? "geracao_indisponivel" : "resultado_invalido";
                    inbox.Estado = "rejeitada";
                }
            }
            await unidade.SalvarAlteracoesAsync(token);
            LogResultado(inbox.Estado);
        }, ct);
    }
    [LoggerMessage(Level = LogLevel.Information, Message = "Resultado de relatório semanal registrado: {Estado}")]
    private partial void LogResultado(string estado);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Resultado de relatório sem trabalho correspondente ignorado")]
    private partial void LogIgnorado();
}
