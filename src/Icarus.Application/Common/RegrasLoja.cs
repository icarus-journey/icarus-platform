using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

internal static class RegrasLoja
{
    public const int LimiteProtecoes = 3;

    public static async Task<Item> BuscarItemAsync(ILojaRepository repositorio, Guid usuarioId, long itemId, CancellationToken ct)
        => (await repositorio.ListarItensAsync(usuarioId, ct))
            .SingleOrDefault(i => i.Id == itemId && i.Estado != EstadoRecompensa.Excluida)
            ?? throw new OperacaoLojaException("recompensa_nao_encontrada", "Recompensa não encontrada.", true);

    public static async Task<ResgateItem> BuscarResgateAsync(ILojaRepository repositorio, Guid usuarioId, long resgateId, CancellationToken ct)
        => (await repositorio.ListarResgatesAsync(usuarioId, ct)).SingleOrDefault(r => r.Id == resgateId)
            ?? throw new OperacaoLojaException("resgate_nao_encontrado", "Resgate não encontrado.", true);

    public static void ValidarTitular(Guid autenticado, Guid usuarioId)
    {
        if (autenticado != usuarioId) throw new PerfilNaoEncontradoException();
    }

    public static void ValidarManutencao(Item item)
    {
        if (item.ControlaApp != ControlaApp.Nao || item.Estado != EstadoRecompensa.Disponivel)
            throw Conflito("estado_invalido", "Somente recompensas disponíveis podem ser alteradas ou excluídas.");
    }

    public static void ValidarDados(DadosRecompensa dados)
    {
        if (string.IsNullOrWhiteSpace(dados.Titulo) || dados.Titulo.Length > 120 ||
            dados.Descricao?.Length > 1000 || dados.Custo is < 1 or > 1_000_000)
            throw Conflito("dados_invalidos", "Informe título de até 120 caracteres, descrição de até 1.000 e custo entre 1 e 1.000.000.");
    }

    public static OperacaoLojaException Conflito(string codigo, string mensagem) => new(codigo, mensagem);
}
