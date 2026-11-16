using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.RelatoSono;

public sealed record RelatoSonoResult(int? HabitualMinutos, int? MetaMinutos, Guid Versao, DateTimeOffset? InformadoEm,
    int? DiferencaParaMetaMinutos, string VersaoContrato = "1.0.0");
public sealed record AtualizarRelatoSonoCommand(Guid Autenticado, Guid Titular, int? HabitualMinutos, int? MetaMinutos, Guid Versao);

public sealed class RelatoSonoUseCase(IUsuarioRepository usuarios, IUnitOfWork unidade, TimeProvider relogio)
{
    public async Task<RelatoSonoResult> ConsultarAsync(Guid autenticado, Guid titular, CancellationToken ct)
        => Apresentar(await BuscarAsync(autenticado, titular, ct));

    public async Task<RelatoSonoResult> AtualizarAsync(AtualizarRelatoSonoCommand pedido, CancellationToken ct)
    {
        var usuario = await BuscarAsync(pedido.Autenticado, pedido.Titular, ct);
        if (usuario.VersaoRelatoSono != pedido.Versao) throw new DadosAtualizadosException(new InvalidOperationException());
        usuario.SonoHabitualMinutos = pedido.HabitualMinutos;
        usuario.MetaSonoMinutos = pedido.MetaMinutos;
        usuario.SonoInformadoEm = pedido.HabitualMinutos is null ? null : relogio.GetUtcNow();
        usuario.VersaoRelatoSono = Guid.NewGuid();
        await unidade.SalvarAlteracoesAsync(ct);
        return Apresentar(usuario);
    }

    private async Task<Icarus.Domain.Entities.Usuario> BuscarAsync(Guid autenticado, Guid titular, CancellationToken ct)
    {
        if (autenticado != titular) throw new PerfilNaoEncontradoException();
        return await usuarios.BuscarPorIdAsync(titular, ct) ?? throw new PerfilNaoEncontradoException();
    }

    public static RelatoSonoResult Apresentar(Icarus.Domain.Entities.Usuario usuario) => new(
        usuario.SonoHabitualMinutos, usuario.MetaSonoMinutos, usuario.VersaoRelatoSono, usuario.SonoInformadoEm,
        usuario.MetaSonoMinutos - usuario.SonoHabitualMinutos);
}
