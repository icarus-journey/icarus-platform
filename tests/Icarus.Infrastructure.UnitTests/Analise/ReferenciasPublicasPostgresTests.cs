using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConsultarReferenciasPublicas;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class ReferenciasPublicasPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly JsonSerializerOptions Opcoes = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static ConsultarReferenciasPublicasUseCase Consulta(IcarusDbContext contexto) =>
        new(new UsuarioRepository(contexto), new EpicoRepository(contexto), new ReferenciasPublicasRepository(contexto), new Relogio());
    private sealed class Relogio : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    }

    private async Task PublicarAsync(string versao)
    {
        await using var contexto = banco.NovoContexto();
        await contexto.Database.ExecuteSqlRawAsync("""
            CREATE SCHEMA IF NOT EXISTS dados_publicos;
            CREATE TABLE IF NOT EXISTS dados_publicos.snapshots(versao_gold text PRIMARY KEY, checksum text, metadados jsonb);
            CREATE TABLE IF NOT EXISTS dados_publicos.snapshot_vigente(chave bool PRIMARY KEY, versao_gold text);
            CREATE TABLE IF NOT EXISTS dados_publicos.referencias(versao_gold text, id text, conteudo jsonb);
            CREATE TABLE IF NOT EXISTS dados_publicos.evidencias_pesquisa(versao_gold text, id text, conteudo jsonb);
            """);
        var metadados = JsonSerializer.Serialize(new MetadadosPublicos(versao, "silver", "2.0.0", "2.0.0", "2.0.0", [
            new("atus_activity", "2024", "https://www.bls.gov/tus/datafiles/atusact-2024.zip", new string('a', 64)),
            new("vigitel_harmonizado", "2006-2024", "https://svs.aids.gov.br/arquivo.zip", new string('b', 64))]), Opcoes);
        await contexto.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dados_publicos.snapshots VALUES ({versao}, {versao}, CAST({metadados} AS jsonb))");
        foreach (var (indicador, fonte, medida) in new[] {
            ("atus_sono_diario", "ATUS", "duracao_participantes"),
            ("vigitel_duracao_sono_habitual", "Vigitel", "duracao_habitual") })
        {
            var json = JsonSerializer.Serialize(new ReferenciaPublica
            {
                Id = indicador,
                Indicador = indicador,
                Fonte = fonte,
                Medida = medida,
                AreaDaVida = "SONO_E_RECUPERACAO",
                FaixaEtaria = fonte == "ATUS" ? "15–17" : "18+",
                TipoDia = "todos",
                Edicao = 2024,
                Titulo = "Sono",
                Populacao = fonte == "ATUS" ? "População dos EUA" : "Adultos das capitais e DF",
                Denominador = "Respostas válidas",
                Valores = new() { Mediana = 420, P25 = 360, P75 = 480, Unidade = "minutos" },
                Limitacoes = ["Não é medição pessoal"]
            }, Opcoes);
            // Campos internos extras nunca devem alcançar o contrato HTTP tipado.
            json = json[..^1] + ",\"n\":5,\"peso\":123,\"identificador_individual\":\"proibido\"}";
            await contexto.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dados_publicos.referencias VALUES ({versao}, {indicador}, CAST({json} AS jsonb))");
        }
        await contexto.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dados_publicos.snapshot_vigente VALUES (true, {versao}) ON CONFLICT (chave) DO UPDATE SET versao_gold=EXCLUDED.versao_gold");
    }

    [FatoPostgres]
    public async Task ConsultaSomenteVigentePreservaLinhagemENaoPublicaCelulasOuIdentificadores()
    {
        await PublicarAsync("primeira");
        await PublicarAsync("segunda");
        var id = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        var resultado = await Consulta(contexto).ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default);
        Assert.Equal("segunda", resultado.VersaoGold);
        var referencia = Assert.Single(resultado.Referencias);
        Assert.Equal("Vigitel", referencia.Dados.Fonte);
        Assert.Equal(new string('b', 64), Assert.Single(referencia.Origens).Sha256);
        Assert.True(referencia.Recorte.Ampliado);
        Assert.Equal("18+", referencia.Recorte.FaixaEfetiva);
        var http = JsonSerializer.Serialize(resultado, JsonSerializerOptions.Web);
        Assert.DoesNotContain("identificador_individual", http);
        Assert.DoesNotContain("\"n\":", http);
        Assert.DoesNotContain("\"peso\":", http);
    }

    [FatoPostgres]
    public async Task IsolaTitularesENaoAmpliaMenorParaAdultoNemEquivalePerfilAMatricula()
    {
        await PublicarAsync("adolescente");
        var id = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Id == id);
        usuario.DataNascimento = new DateOnly(2009, 10, 20); usuario.Estuda = true; usuario.Trabalha = false;
        await contexto.SaveChangesAsync();
        var consulta = Consulta(contexto);
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => consulta.ExecuteAsync(new(Guid.NewGuid(), id, "SONO_E_RECUPERACAO"), default));
        var resultado = await consulta.ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default);
        var referencia = Assert.Single(resultado.Referencias);
        Assert.Equal("ATUS", referencia.Dados.Fonte);
        Assert.Null(referencia.Recorte.FiltrosEfetivos.Estuda);
        Assert.Null(referencia.Recorte.FiltrosEfetivos.Trabalha);
        Assert.Contains(referencia.Recorte.Avisos, a => a.Contains("perguntas do perfil", StringComparison.Ordinal));
        Assert.Equal("sem_mapeamento", (await consulta.ExecuteAsync(new(id, id, "FINANCAS"), default)).Estado);
        Assert.Equal("escolha_area", (await consulta.ExecuteAsync(new(id, id), default)).Estado);
    }

    [FatoPostgres]
    public async Task AusenciaDeSnapshotNaoViraZeroEPublicacaoAbortadaMantemVigente()
    {
        await PublicarAsync("vigente-estavel");
        await using (var tentativa = banco.NovoContexto())
        {
            await using var transacao = await tentativa.Database.BeginTransactionAsync();
            await tentativa.Database.ExecuteSqlRawAsync("UPDATE dados_publicos.snapshot_vigente SET versao_gold='nao-publicado'");
            await transacao.RollbackAsync();
        }
        var id = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        Assert.Equal("vigente-estavel", (await Consulta(contexto).ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default)).VersaoGold);
        await contexto.Database.ExecuteSqlRawAsync("DELETE FROM dados_publicos.snapshot_vigente");
        var resultado = await Consulta(contexto).ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default);
        Assert.Equal("indisponivel", resultado.Estado);
        Assert.Empty(resultado.Referencias);
        Assert.Null(resultado.VersaoGold);
    }

    [FatoPostgres]
    public async Task ContextualizaSomenteRelatoExplicitoComPerguntaNoturnaCompativel()
    {
        await PublicarAsync("sono-explicito");
        var id = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        var consulta = Consulta(contexto);
        var semRelato = await consulta.ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default);
        Assert.Null(semRelato.ContextoSono);
        Assert.Null(semRelato.RelatoSono!.HabitualMinutos);
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Id == id);
        usuario.SonoHabitualMinutos = 360; usuario.MetaSonoMinutos = 420; usuario.SonoInformadoEm = DateTimeOffset.UtcNow;
        await contexto.SaveChangesAsync();
        var leitura = await consulta.ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default);
        Assert.Equal("inicio", leitura.ContextoSono!.PosicaoNaFaixa);
        Assert.Equal(60, leitura.RelatoSono!.DiferencaParaMetaMinutos);
        Assert.Equal(360, leitura.ContextoSono.InicioFaixaMinutos);
        Assert.Equal(480, leitura.ContextoSono.FimFaixaMinutos);
        Assert.Equal("vigitel_duracao_sono_habitual", leitura.ContextoSono.ReferenciaId);
        usuario.DataNascimento = new(2009, 10, 20);
        await contexto.SaveChangesAsync();
        Assert.Null((await consulta.ExecuteAsync(new(id, id, "SONO_E_RECUPERACAO"), default)).ContextoSono);
    }

    private static async Task AdicionarAsync(IcarusDbContext contexto, string versao, ReferenciaPublica referencia)
    {
        var json = JsonSerializer.Serialize(referencia, Opcoes);
        await contexto.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dados_publicos.referencias VALUES ({versao}, {referencia.Id}, CAST({json} AS jsonb))");
    }

    [FatoPostgres]
    public async Task CampanhaDoTitularSelecionaRelacoesCruzadasDoMesmoSnapshotSemExporOutraCampanha()
    {
        await PublicarAsync("relacoes-campanha");
        var titular = await banco.CriarUsuarioAsync(0);
        var outro = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        (await contexto.Usuarios.FindAsync(titular))!.DataNascimento = new(2000, 1, 1);
        var epico = new Epico { UsuarioId = titular, Titulo = "Movimento", AreaDaVida = AreaDaVida.CorpoESaude };
        var campanha = new Campanha
        {
            Epico = epico,
            Titulo = "Minha campanha de movimento",
            AreaDaVida = AreaDaVida.CorpoESaude,
            Estado = EstadoCampanha.EmAndamento,
            PrazoFim = new(2027, 1, 1)
        };
        var alheia = new Campanha { Epico = new() { UsuarioId = outro, Titulo = "Privado" }, Titulo = "Privada" };
        contexto.Campanhas.AddRange(campanha, alheia);
        await contexto.SaveChangesAsync();
        foreach (var (indicador, desfecho) in new[] {
            ("vigitel_atividade_sintomas_sono", "sintomas_insonia"),
            ("vigitel_atividade_deseja_mais_sono", "deseja_mais_sono") })
            await AdicionarAsync(contexto, "relacoes-campanha", new()
            {
                Id = indicador,
                Indicador = indicador,
                Medida = "associacao",
                Fonte = "Vigitel",
                Edicao = 2024,
                AreaDaVida = "SONO_E_RECUPERACAO",
                FaixaEtaria = "18–28",
                TipoDia = "todos",
                Valores = new()
                {
                    Unidade = "percentual_de_pessoas_e_pontos_percentuais",
                    Exposicao = "ativo_lazer",
                    Desfecho = desfecho,
                    GrupoExpostoPercentual = 20,
                    GrupoNaoExpostoPercentual = 25
                }
            });
        var consulta = Consulta(contexto);
        var resultado = await consulta.ExecuteAsync(new(titular, titular, CampanhaId: campanha.Id), default);
        Assert.Equal("1.2.0", resultado.VersaoContrato);
        Assert.Equal("CORPO_E_SAUDE", resultado.AreaDaVida);
        Assert.Equal(campanha.Id, resultado.Meta!.Id);
        Assert.Equal("relacoes-campanha", resultado.VersaoGold);
        Assert.Equal(2, resultado.Leituras!.Count);
        Assert.All(resultado.Leituras, l =>
        {
            Assert.Contains("20% menos", l.Leitura);
            Assert.Equal(new string('b', 64), Assert.Single(l.Origens).Sha256);
        });
        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => consulta.ExecuteAsync(new(titular, titular, CampanhaId: alheia.Id), default));
        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => consulta.ExecuteAsync(new(titular, titular, CampanhaId: long.MaxValue), default));
        var sono = await consulta.ExecuteAsync(new(titular, titular, "SONO_E_RECUPERACAO"), default);
        Assert.Equal(2, sono.Leituras!.Count);
        Assert.All(resultado.Leituras, l => Assert.Contains(sono.Leituras, s => s.ReferenciasIds.SequenceEqual(l.ReferenciasIds)));
    }

    [FatoPostgres]
    public async Task ComposicaoAtusLeOsDoisGruposSemInferirOcupacaoPeloPerfil()
    {
        await PublicarAsync("comparacao-estudo");
        var titular = await banco.CriarUsuarioAsync(0);
        await using var contexto = banco.NovoContexto();
        var usuario = (await contexto.Usuarios.FindAsync(titular))!;
        usuario.DataNascimento = new(2000, 1, 1); usuario.Estuda = true; usuario.Trabalha = true;
        await contexto.SaveChangesAsync();
        foreach (var (id, trabalha, percentual) in new (string, bool?, decimal)[] {
            ("amplo", null, 20.7m), ("ocupado", true, 13.6m), ("nao-ocupado", false, 42.1m) })
            await AdicionarAsync(contexto, "comparacao-estudo", new()
            {
                Id = id,
                Indicador = "atus_aulas_tarefas_estudo",
                Fonte = "ATUS",
                Edicao = 2024,
                AreaDaVida = "ESTUDOS_E_APRENDIZADO",
                Medida = "participacao_no_dia",
                FaixaEtaria = "18–28",
                TipoDia = "todos",
                Filtros = new(null, trabalha),
                Valores = new() { Unidade = "percentual_de_pessoas", Percentual = percentual }
            });
        var resultado = await Consulta(contexto).ExecuteAsync(new(titular, titular, "ESTUDOS_E_APRENDIZADO"), default);
        Assert.Equal(2, resultado.Leituras!.Count);
        Assert.Equal(["ocupado", "nao-ocupado"], resultado.Leituras[0].ReferenciasIds);
        Assert.Equal("amplo", Assert.Single(resultado.Leituras[1].ReferenciasIds));
        Assert.Null(Assert.Single(resultado.Referencias).Recorte.FiltrosEfetivos.Trabalha);
        Assert.DoesNotContain("você trabalha", resultado.Leituras[0].Recomendacao.Texto, StringComparison.OrdinalIgnoreCase);
    }
}
