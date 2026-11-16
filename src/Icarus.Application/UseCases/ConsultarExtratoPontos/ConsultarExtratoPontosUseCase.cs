using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarExtratoPontos;

public sealed class ConsultarExtratoPontosUseCase(IExtratoPontosRepository repositorio, IUsuarioRepository usuarios,
    ICursorExtratoPontos cursores, TimeProvider relogio)
{
    private async Task<string> Autorizar(Guid autenticado, Guid titular, CancellationToken ct)
    {
        if (autenticado != titular) throw new PerfilNaoEncontradoException();
        var usuario = await usuarios.BuscarPorIdAsync(titular, ct) ?? throw new PerfilNaoEncontradoException();
        return FusoHorario.EhValido(usuario.FusoHorario) ? usuario.FusoHorario! : "UTC";
    }
    public async Task<PaginaExtratoPontos> ExecuteAsync(ConsultaExtratoPontos c, CancellationToken ct)
    {
        var fuso = await Autorizar(c.Autenticado, c.Titular, ct);
        var agora = relogio.GetUtcNow();
        var cursor = c.Cursor is null ? null : cursores.Decodificar(c.Cursor);
        var hoje = FusoHorario.DataLocal(agora, fuso);
        var filtro = new FiltroExtratoPontos(c.TodoHistorico ? null : c.Inicio ?? cursor?.Filtro.Inicio ?? hoje.AddDays(-29),
            c.TodoHistorico ? null : c.Fim ?? cursor?.Filtro.Fim ?? hoje, c.TodoHistorico, c.Natureza, c.Limite);
        if ((c.Inicio is null) != (c.Fim is null) || (c.TodoHistorico && c.Inicio is not null)
            || filtro.Fim == DateOnly.MaxValue || filtro.Inicio > filtro.Fim || (filtro.Fim?.DayNumber - filtro.Inicio?.DayNumber) > 3660
            || c.Limite is < 1 or > 50 || !Enum.IsDefined(c.Natureza)) throw new ConsultaExtratoInvalidaException();
        if (cursor is not null && (cursor.UsuarioId != c.Titular || cursor.Filtro != filtro || cursor.FusoHorario != fuso
            || cursor.ConsultadoEm > agora || cursor.ConsultadoEm < agora.AddHours(-24)
            || cursor.UltimaData > cursor.ConsultadoEm || cursor.UltimoId <= 0 || cursor.LimiteId < cursor.UltimoId))
            throw new ConsultaExtratoInvalidaException();
        var referencia = cursor?.ConsultadoEm ?? agora;
        var lote = await repositorio.ListarAsync(c.Titular, filtro, referencia, fuso, cursor, ct);
        var ultimo = lote.Movimentos.Count == 0 ? null : lote.Movimentos[^1];
        var proximo = lote.TemMais && ultimo is not null
            ? cursores.Codificar(new(c.Titular, filtro, ultimo.RegistradoEm, ultimo.Id, lote.LimiteId, referencia, fuso)) : null;
        return new("1.0.0", filtro, fuso, referencia, lote.SaldoAtual, lote.Movimentos, proximo, lote.LimiteId > 0);
    }
    public async Task<DetalheExtratoPontos> ConsultarAsync(Guid autenticado, Guid titular, long id, CancellationToken ct)
    {
        var fuso = await Autorizar(autenticado, titular, ct);
        var m = await repositorio.ConsultarAsync(titular, id, fuso, ct) ?? throw new PerfilNaoEncontradoException();
        return new("1.0.0", fuso, m);
    }
}
