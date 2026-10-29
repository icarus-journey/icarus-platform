using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.ConsultarLoja;

public sealed class ConsultarLojaUseCase(ILojaRepository repositorio, LojaOptions configuracao)
{
    public async Task<LojaResult> ExecuteAsync(ConsultarLojaCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        LojaResult? resultado = null;
        await repositorio.ExecutarAsync(usuarioId, async (usuario, token) =>
        {
            var itens = await repositorio.ListarItensAsync(usuarioId, token);
            var resgates = await repositorio.ListarResgatesAsync(usuarioId, token);
            resultado = new LojaResult(usuario.SaldoPontos,
                itens.Where(i => i.ControlaApp == ControlaApp.Nao && i.Estado != EstadoRecompensa.Excluida)
                    .OrderByDescending(i => i.Id)
                    .Select(i => new RecompensaResult(i.Id, i.Nome, i.Descricao, i.Valor, i.Estado,
                        i.PrimeiroResgateEm is not null,
                        resgates.Where(r => r.ItemId == i.Id && r.Estado == EstadoResgate.Resgatado)
                            .Select(r => (long?)r.Id).FirstOrDefault())).ToList(),
                new ProtecaoResult("Proteção de sequência", configuracao.CustoProtecao,
                    resgates.Count(r => r.Protecao && r.Estado == EstadoResgate.Resgatado), LimiteProtecoes),
                resgates.OrderByDescending(r => r.Id).Select(r => new ResgateResult(r.Id, r.ItemId,
                    r.Titulo, r.Descricao, r.Custo, r.Protecao, r.Estado, r.ResgatadoEm,
                    r.EstornadoEm, r.UtilizadoEm)).ToList());
        }, ct);
        return resultado!;
    }

}
