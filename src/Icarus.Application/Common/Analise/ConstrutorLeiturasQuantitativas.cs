using System.Globalization;

namespace Icarus.Application.Common.Analise;

/// <summary>Redige somente relações previstas no catálogo, sobre agregados públicos do mesmo snapshot.</summary>
public static class ConstrutorLeiturasQuantitativas
{
    private static readonly CultureInfo Portugues = CultureInfo.GetCultureInfo("pt-BR");
    private const string Aulas = "atus_aulas_tarefas_estudo";

    public static IReadOnlyList<LeituraQuantitativa> Construir(SnapshotReferenciasPublicas snapshot,
        string area, int idade, string tipoDia, MetaPesquisa? meta)
    {
        var leituras = new List<LeituraQuantitativa>();
        if (area is "CORPO_E_SAUDE" or "SONO_E_RECUPERACAO")
        {
            AdicionarSono("vigitel_atividade_sintomas_sono", "Atividade física e sono",
                "Relataram sintomas frequentes de insônia", "sintomas de insônia",
                "Ver relação com os sintomas de insônia", "sintomas_insonia");
            AdicionarSono("vigitel_atividade_deseja_mais_sono", "Atividade física e vontade de dormir mais",
                "Gostariam de dormir mais que o habitual", "vontade de dormir mais",
                "Ver relação com a vontade de dormir mais", "deseja_mais_sono");
        }
        if (area == "ESTUDOS_E_APRENDIZADO")
        {
            AdicionarOcupacao();
            AdicionarParticipacao(Aulas, "Participação em estudos", "Registraram aulas ou tarefas naquele dia",
                "aulas ou tarefas", "Ver participação em estudos", new("Dê espaço ao seu próximo estudo",
                    meta is null ? "Escolha uma tarefa de estudo e um dia em que ela caiba nos seus compromissos."
                        : "Para continuar sua campanha, escolha uma missão de estudo e um dia em que ela caiba nos seus compromissos."));
        }
        // As outras áreas mantêm contexto quantitativo, sem fabricar uma associação entre hábitos.
        if (leituras.Count == 0)
        {
            var conteudo = area switch
            {
                "CORPO_E_SAUDE" => ("vigitel_atividade_fisica_lazer", "Atividade física no lazer", "Relataram atividade física suficiente no lazer", "atividade física suficiente", "Movimento que caiba na sua rotina", "Se sua campanha envolve atividade física, escolha um próximo passo possível na sua rotina."),
                "SONO_E_RECUPERACAO" => ("atus_sono_diario", "Sono no diário", "Registraram sono ou cochilos naquele dia", "sono ou cochilos", "Dê espaço ao descanso", "Ao planejar sua campanha, considere também o espaço para descanso entre os compromissos."),
                "TRABALHO_E_CARREIRA" => ("atus_trabalho_empregos", "Trabalho na rotina", "Registraram trabalho naquele dia", "trabalho", "Planeje junto aos seus compromissos", "Ao escolher a próxima missão da campanha, considere os compromissos de trabalho que já existem na sua rotina."),
                "CASA_E_VIDA_PRATICA" => ("atus_casa_alimentacao", "Cuidados com a casa", "Registraram tarefas domésticas ou preparo de alimentos naquele dia", "tarefas domésticas ou preparo de alimentos", "Dê espaço às tarefas da casa", "Escolha uma tarefa da sua campanha que caiba entre os demais compromissos do dia."),
                "LAZER_E_CRIATIVIDADE" => ("atus_lazer_selecionado", "Lazer na rotina", "Registraram atividades de lazer selecionadas naquele dia", "atividades de lazer selecionadas", "Reserve espaço para o que você gosta", "Se sua campanha envolve lazer ou criação, escolha um momento possível para uma atividade que você quer manter."),
                _ => default
            };
            if (conteudo.Item1 is not null)
                AdicionarParticipacao(conteudo.Item1, conteudo.Item2, conteudo.Item3, conteudo.Item4,
                    "Ver participação na pesquisa", new(conteudo.Item5, conteudo.Item6));
        }
        return leituras;

        void AdicionarSono(string indicador, string titulo, string medida, string desfecho, string atalho, string codigoDesfecho)
        {
            var referencia = SelecionadorReferenciasPublicas.Selecionar(snapshot.Referencias, indicador, "associacao", idade, tipoDia: tipoDia);
            if (referencia is not { Fonte: "Vigitel", Valores: { Exposicao: "ativo_lazer", Unidade: "percentual_de_pessoas_e_pontos_percentuais" } valores }
                || valores.Desfecho != codigoDesfecho
                || !PercentualValido(valores.GrupoExpostoPercentual) || !PercentualValido(valores.GrupoNaoExpostoPercentual)) return;
            var ativo = valores.GrupoExpostoPercentual!.Value;
            var outro = valores.GrupoNaoExpostoPercentual!.Value;
            var leitura = Comparar(ativo, outro, "No grupo com atividade física suficiente, houve", $"relatos de {desfecho}");
            var recomendacao = ativo < outro
                ? new RecomendacaoDaPesquisa("O movimento pode ser um aliado",
                    area == "CORPO_E_SAUDE"
                        ? $"Se sua campanha inclui atividade física e dormir melhor também é um objetivo seu, considere manter um movimento que caiba na sua rotina. O grupo ativo da pesquisa apresentou menos relatos de {desfecho}."
                        : $"Se você busca dormir melhor, considere reservar um momento para uma atividade física que caiba na sua rotina. O grupo ativo da pesquisa apresentou menos relatos de {desfecho}.")
                : new RecomendacaoDaPesquisa("Um passo possível na sua rotina",
                    "Se sua campanha inclui movimento, escolha uma atividade que caiba na sua rotina. Esta comparação descreve os grupos da pesquisa.");
            var dados = $"{Populacao(referencia)}. {medida}: {Numero(ativo)}% no grupo com atividade física suficiente no lazer e {Numero(outro)}% no grupo sem atividade suficiente. Associação observada, sem calcular o efeito da atividade para uma pessoa.";
            Adicionar(indicador, titulo, medida, atalho,
                [new("Com atividade física suficiente no lazer", ativo), new("Sem atividade física suficiente no lazer", outro)],
                leitura, recomendacao, dados, [referencia]);
        }

        void AdicionarOcupacao()
        {
            // O par deve existir por inteiro no mesmo recorte. Nunca comparar filtros sobrepostos
            // nem aceitar o fallback de um grupo ocupado para o grupo sem filtro de ocupação.
            foreach (var dia in tipoDia == "todos" ? new[] { "todos" } : [tipoDia, "todos"])
                foreach (var faixa in SelecionadorReferenciasPublicas.FaixasCompativeis(idade))
                {
                    ReferenciaPublica? Grupo(bool trabalha) => snapshot.Referencias.FirstOrDefault(r =>
                        r.Indicador == Aulas && r.Medida == "participacao_no_dia" && r.Fonte == "ATUS"
                        && r.TipoDia == dia && r.FaixaEtaria == faixa && r.Filtros == new FiltrosReferencia(null, trabalha)
                        && r.Valores.Unidade == "percentual_de_pessoas" && PercentualValido(r.Valores.Percentual));
                    var ocupado = Grupo(true);
                    var naoOcupado = Grupo(false);
                    if (ocupado is null || naoOcupado is null || ocupado.Edicao != naoOcupado.Edicao
                        || ocupado.Populacao != naoOcupado.Populacao || ocupado.Denominador != naoOcupado.Denominador) continue;
                    var a = ocupado.Valores.Percentual!.Value;
                    var b = naoOcupado.Valores.Percentual!.Value;
                    Adicionar("estudo_ocupacao", "Estudo e trabalho", "Registraram aulas ou tarefas naquele dia",
                        "Ver relação com trabalho", [new("Pessoas ocupadas", a), new("Pessoas não ocupadas", b)],
                        Comparar(a, b, "Entre os ocupados, houve", "participação em aulas ou tarefas naquele dia"),
                        new("Faça o estudo caber na sua rotina", "Se você concilia estudo e trabalho, escolha uma missão de estudo e deixe definido um momento possível para começar."),
                        $"{Populacao(ocupado)}. Registraram aulas ou tarefas naquele dia: {Numero(a)}% dos ocupados e {Numero(b)}% dos não ocupados. Os grupos também podem diferir em matrícula e outros fatores; a comparação não isola um efeito do trabalho.",
                        [ocupado, naoOcupado]);
                    return;
                }
        }

        void AdicionarParticipacao(string indicador, string titulo, string medida, string atividade, string atalho, RecomendacaoDaPesquisa recomendacao)
        {
            var medidaPublica = indicador.StartsWith("vigitel_", StringComparison.Ordinal) ? "percentual" : "participacao_no_dia";
            var referencia = SelecionadorReferenciasPublicas.Selecionar(snapshot.Referencias, indicador, medidaPublica, idade, tipoDia: tipoDia);
            if (referencia is null || referencia.Valores.Unidade != "percentual_de_pessoas"
                || !PercentualValido(referencia.Valores.Percentual)) return;
            var percentual = referencia.Valores.Percentual!.Value;
            var noDia = referencia.Fonte == "ATUS" ? " naquele dia" : "";
            var leitura = Participacao(percentual, atividade, noDia);
            Adicionar($"{indicador}_participacao", titulo, medida, atalho,
                [new("Registraram na pesquisa", percentual), new("Não registraram na pesquisa", 100 - percentual)],
                leitura, recomendacao, $"{Populacao(referencia)}. {medida}: {Numero(percentual)}%. "
                    + (referencia.Fonte == "ATUS" ? "Cada pessoa relatou um dia de 24 horas; isso não mede a frequência semanal." : "Estimativa a partir das respostas à pesquisa."), [referencia]);
        }

        void Adicionar(string id, string titulo, string medida, string atalho, IReadOnlyList<GrupoDaPesquisa> grupos,
            string leitura, RecomendacaoDaPesquisa recomendacao, string dados, ReferenciaPublica[] referencias)
        {
            var referencia = referencias[0];
            var prefixo = referencia.Fonte == "ATUS" ? "atus_" : "vigitel_";
            var origens = snapshot.Metadados.Fontes.Where(f => f.Fonte.StartsWith(prefixo, StringComparison.Ordinal)).ToArray();
            if (origens.Length == 0) return;
            var url = referencia.Fonte == "ATUS" ? "https://www.bls.gov/tus/" : "https://svs.aids.gov.br/daent/cgdnt/vigitel/";
            leituras.Add(new(id, titulo, medida, atalho, grupos, leitura, recomendacao, dados,
                $"{referencia.Fonte} {referencia.Edicao} · cálculo ponderado do Icarus sobre a base oficial.", url,
                referencia.FaixaEtaria, referencia.TipoDia, referencias.Select(r => r.Id).ToArray(), origens,
                referencias.SelectMany(r => r.Limitacoes).Distinct().ToArray()));
        }
    }

    private static bool PercentualValido(decimal? valor) => valor is >= 0 and <= 100;
    private static string Numero(decimal valor) => valor.ToString("0.#", Portugues);
    private static string Populacao(ReferenciaPublica referencia)
    {
        var faixa = referencia.FaixaEtaria.EndsWith('+')
            ? $"a partir de {referencia.FaixaEtaria.TrimEnd('+')} anos" : $"de {referencia.FaixaEtaria} anos";
        var local = referencia.Fonte == "ATUS" ? "da população civil não institucionalizada dos Estados Unidos" : "das 26 capitais e do Distrito Federal";
        var dia = referencia.TipoDia switch { "segunda_sexta" => " · segunda a sexta", "sabado_domingo" => " · sábado e domingo", _ => " · todos os dias da semana" };
        return $"Pessoas {faixa} {local}{dia}";
    }

    private static string Comparar(decimal valor, decimal baseComparacao, string inicio, string fim)
    {
        if (valor == baseComparacao) return "Os dois grupos apresentaram a mesma proporção nos valores publicados.";
        if (baseComparacao == 0) return "O segundo grupo não teve percentual registrado; não cabe calcular uma diferença relativa.";
        var relativa = Math.Abs((valor - baseComparacao) / baseComparacao * 100);
        var arredondada = Math.Round(relativa, relativa < 1 ? 1 : 0, MidpointRounding.AwayFromZero);
        if (arredondada == 0) return "Os dois grupos apresentaram proporções próximas nos valores publicados.";
        return $"{inicio} cerca de {Numero(arredondada)}% {(valor < baseComparacao ? "menos" : "mais")} {fim}.";
    }

    private static string Participacao(decimal percentual, string atividade, string noDia)
    {
        if (percentual is > 0 and < 100)
        {
            var pessoas = (int)Math.Round(100 / percentual, MidpointRounding.AwayFromZero);
            if (pessoas is >= 2 and <= 20 && Math.Abs(100m / pessoas - percentual) / percentual <= 0.05m)
                return $"Cerca de 1 em cada {pessoas} pessoas registrou {atividade}{noDia}.";
        }
        return $"Na pesquisa, {Numero(percentual)}% das pessoas registraram {atividade}{noDia}.";
    }
}
