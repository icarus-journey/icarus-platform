using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
namespace Icarus.Application.UseCases.ConsultarHistoricoRelatorios;

public sealed class ConsultarHistoricoRelatoriosUseCase(IHistoricoRelatoriosRepository repositorio,
    IUsuarioRepository usuarios, ICursorHistoricoRelatorios cursores, TimeProvider relogio)
{
    private async Task Autorizar(Guid autenticado, Guid titular, CancellationToken ct)
    {
        if (autenticado != titular || await usuarios.BuscarPorIdAsync(titular, ct) is null) throw new PerfilNaoEncontradoException();
    }
    public async Task<PaginaHistoricoRelatorios> ExecuteAsync(ConsultaHistoricoRelatorios c, CancellationToken ct)
    {
        await Autorizar(c.Autenticado, c.Titular, ct);
        if (c.Limite is < 1 or > 50) throw new ConsultaHistoricoRelatoriosInvalidaException();
        var cursor = c.Cursor is null ? null : cursores.Decodificar(c.Cursor);
        var agora = relogio.GetUtcNow();
        if (cursor is not null && (cursor.UsuarioId != c.Titular || cursor.Limite != c.Limite
            || cursor.ConsultadoEm > agora || cursor.ConsultadoEm < agora.AddHours(-24)
            || cursor.UltimoId <= 0 || cursor.LimiteId < cursor.UltimoId)) throw new ConsultaHistoricoRelatoriosInvalidaException();
        var referencia = cursor?.ConsultadoEm ?? agora;
        var lote = await repositorio.ListarAsync(c.Titular, c.Limite, referencia, cursor, ct);
        var ultimo = lote.Relatorios.Count == 0 ? null : lote.Relatorios[^1];
        var proximo = lote.TemMais && ultimo is not null
            ? cursores.Codificar(new(c.Titular, c.Limite, ultimo.Periodo.Inicio, ultimo.Id, lote.LimiteId, referencia)) : null;
        return new("1.0.0", referencia, lote.Relatorios, proximo);
    }
    public async Task<DetalheRelatorioSalvo> ConsultarAsync(Guid autenticado, Guid titular, long id, CancellationToken ct)
    {
        await Autorizar(autenticado, titular, ct);
        return await repositorio.ConsultarAsync(titular, id, ct) ?? throw new PerfilNaoEncontradoException();
    }
}
