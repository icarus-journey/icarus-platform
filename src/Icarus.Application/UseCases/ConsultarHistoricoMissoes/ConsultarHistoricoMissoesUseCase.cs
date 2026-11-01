using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarHistoricoMissoes;

public sealed class ConsultarHistoricoMissoesUseCase(IHistoricoMissoesRepository repositorio, IUsuarioRepository usuarios,
    ICursorHistoricoMissoes cursores, TimeProvider relogio)
{
    private async Task<string> Autorizar(Guid autenticado, Guid titular, CancellationToken ct)
    {
        if (autenticado != titular) throw new MissaoNaoEncontradaException();
        var usuario = await usuarios.BuscarPorIdAsync(titular, ct) ?? throw new MissaoNaoEncontradaException();
        return FusoHorario.EhValido(usuario.FusoHorario) ? usuario.FusoHorario! : "UTC";
    }
    public async Task<PaginaHistoricoMissoes> ExecuteAsync(ConsultaHistoricoMissoes c, CancellationToken ct)
    {
        var fuso = await Autorizar(c.Autenticado, c.Titular, ct);
        var agora = relogio.GetUtcNow();
        var cursor = c.Cursor is null ? null : cursores.Decodificar(c.Cursor);
        var hoje = FusoHorario.DataLocal(agora, fuso);
        var filtro = new FiltroHistoricoMissoes(c.Inicio ?? cursor?.Filtro.Inicio ?? hoje.AddDays(-29), c.Fim ?? cursor?.Filtro.Fim ?? hoje,
            c.Estado, c.CampanhaId, c.Avulsas, c.Limite);
        if ((c.Inicio is null) != (c.Fim is null) || filtro.Inicio > filtro.Fim || filtro.Fim.DayNumber - filtro.Inicio.DayNumber > 3660
            || filtro.Limite is < 1 or > 50 || !Enum.IsDefined(filtro.Estado) || filtro.CampanhaId <= 0 || (filtro.Avulsas && filtro.CampanhaId is not null))
            throw new ConsultaHistoricoInvalidaException();
        if (cursor is not null && (cursor.UsuarioId != c.Titular || cursor.Filtro != filtro || cursor.FusoHorario != fuso
            || cursor.ConsultadoEm > agora || cursor.ConsultadoEm < agora.AddHours(-24) || cursor.UltimoId <= 0 || cursor.LimiteId < cursor.UltimoId
            || cursor.UltimaData < filtro.Inicio || cursor.UltimaData > filtro.Fim)) throw new ConsultaHistoricoInvalidaException();
        var referencia = cursor?.ConsultadoEm ?? agora;
        var lote = await repositorio.ListarAsync(c.Titular, filtro, referencia, fuso, cursor, ct);
        var ultimo = lote.Registros.Count == 0 ? null : lote.Registros[^1];
        var proximo = lote.TemMais && ultimo is not null ? cursores.Codificar(new(c.Titular, filtro, ultimo.DataPrevista, ultimo.Id, lote.LimiteId, referencia, fuso)) : null;
        return new("1.0.0", filtro, fuso, referencia, lote.Registros, proximo, lote.HistoricoParcial, lote.LimiteId > 0 || lote.HistoricoParcial);
    }
    public async Task<DetalheHistoricoMissao> ConsultarAsync(Guid autenticado, Guid titular, long id, CancellationToken ct)
    {
        var fuso = await Autorizar(autenticado, titular, ct);
        var r = await repositorio.ConsultarAsync(titular, id, FusoHorario.DataLocal(relogio.GetUtcNow(), fuso), ct) ?? throw new MissaoNaoEncontradaException();
        return new("1.0.0", fuso, r);
    }
    public async Task<IReadOnlyList<CampanhaHistoricoMissao>> ListarCampanhasAsync(Guid autenticado, Guid titular, CancellationToken ct)
    {
        await Autorizar(autenticado, titular, ct);
        return await repositorio.ListarCampanhasAsync(titular, ct);
    }
}
