using Icarus.Application.Common.Analise;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ConsultarReferenciasPublicas;

public sealed class ConsultarReferenciasPublicasUseCase(IUsuarioRepository usuarios,
    IEpicoRepository epicos, IReferenciasPublicasRepository referencias, TimeProvider relogio)
{
    public async Task<ConsultarReferenciasPublicasResult> ExecuteAsync(ConsultarReferenciasPublicasCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var usuario = await usuarios.BuscarPorIdAsync(command.UsuarioIdSolicitado, ct) ?? throw new PerfilNaoEncontradoException();
        var codigo = command.AreaDaVida;
        if (codigo is null)
        {
            var lista = await epicos.ListarComCampanhasEMissoesAsync(usuario.Id, ct);
            var area = lista.FirstOrDefault(e => e.Estado == EstadoEpico.EmAndamento)?.AreaDaVida;
            codigo = CatalogoAreasAnaliticas.Areas.FirstOrDefault(a => a.Area == area)?.Codigo;
        }
        if (codigo is null) return Vazio("escolha_area", null, "Escolha uma área para conhecer seu contexto público.");
        var titulo = CatalogoAreasAnaliticas.Areas.Single(a => a.Codigo == codigo).Titulo;
        if (!CatalogoAreasAnaliticas.EhMapeada(codigo))
            return Vazio("sem_mapeamento", codigo, $"Referência pública compatível de {titulo} em breve no Icarus.");
        var agora = relogio.GetUtcNow();
        if (usuario.FusoHorario is not null && TimeZoneInfo.TryFindSystemTimeZoneById(usuario.FusoHorario, out var fuso))
            agora = TimeZoneInfo.ConvertTime(agora, fuso);
        var hoje = DateOnly.FromDateTime(agora.DateTime);
        var idade = hoje.Year - usuario.DataNascimento.Year;
        if (usuario.DataNascimento.AddYears(idade) > hoje) idade--;
        if (idade < 15) return Vazio("sem_referencia_compativel", codigo, "Ainda não temos referências públicas compatíveis com sua faixa etária.");
        var snapshot = await referencias.ObterVigenteAsync(codigo, ct);
        if (snapshot is null || snapshot.Metadados.VersaoContrato != "2.0.0")
            return Vazio("indisponivel", codigo, "As referências públicas estão temporariamente indisponíveis. Tente novamente mais tarde.");
        // As perguntas atuais não demonstram equivalência com matrícula e ocupação ATUS.
        // O algoritmo aceita filtros compatíveis, mas esta versão não infere esses filtros do perfil.
        var prioridades = snapshot.Referencias.Select(r => (r.Indicador, r.Medida)).Distinct()
            .OrderBy(r => Prioridade(r.Indicador, r.Medida)).ThenBy(r => r.Indicador, StringComparer.Ordinal)
            .ThenBy(r => r.Medida, StringComparer.Ordinal);
        var selecionadas = new List<ReferenciaSelecionada>();
        foreach (var (indicador, medida) in prioridades)
        {
            var referencia = SelecionadorReferenciasPublicas.Selecionar(snapshot.Referencias, indicador, medida, idade, tipoDia: command.TipoDia);
            if (referencia is null) continue;
            var avisos = new List<string>();
            var faixa = SelecionadorReferenciasPublicas.FaixaDaIdade(idade)!;
            if (referencia.FaixaEtaria != faixa) avisos.Add("Referência de uma faixa etária mais ampla, identificada abaixo.");
            if (referencia.TipoDia != command.TipoDia) avisos.Add("Referência de todos os dias, pois não há recorte compatível para o tipo de dia escolhido.");
            if (usuario.Estuda is not null || usuario.Trabalha is not null)
                avisos.Add("Grupo amplo, sem filtro de estudo ou trabalho; as perguntas do perfil não equivalem às desta pesquisa.");
            var origens = snapshot.Metadados.Fontes.Where(f => f.Fonte.StartsWith(referencia.Fonte == "ATUS" ? "atus_" : "vigitel_", StringComparison.Ordinal)).ToArray();
            if (origens.Length == 0) continue;
            selecionadas.Add(new(referencia, new(faixa, referencia.FaixaEtaria, command.TipoDia, referencia.TipoDia,
                referencia.Filtros, avisos.Count > 0, avisos), referencia.Fonte == "ATUS" ? "Estados Unidos" : "Brasil", origens));
            if (selecionadas.Count == 4) break;
        }
        return new(selecionadas.Count > 0 ? "disponivel" : "sem_referencia_compativel", codigo,
            selecionadas.Count > 0 ? "Contexto público de 2024; não é uma avaliação da sua rotina." : "Ainda não temos uma referência pública compatível para este recorte.",
            snapshot.Metadados.VersaoGold, snapshot.Hash, selecionadas, VersaoCatalogo: snapshot.Metadados.VersaoCatalogo);
    }

    private static ConsultarReferenciasPublicasResult Vazio(string estado, string? area, string mensagem)
        => new(estado, area, mensagem, null, null, []);

    private static int Prioridade(string indicador, string medida) => (indicador, medida) switch
    {
        ("vigitel_duracao_sono_habitual", "duracao_habitual") => 0,
        ("atus_sono_diario", "duracao_participantes") => 1,
        ("vigitel_atividade_sintomas_sono", "associacao") => 2,
        (_, "participacao_no_dia" or "percentual") => 3,
        (_, "duracao_participantes") => 4,
        (_, "distribuicao_periodos") => 5,
        _ => 6
    };
}
