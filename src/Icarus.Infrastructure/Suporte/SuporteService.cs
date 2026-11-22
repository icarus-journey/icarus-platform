using Icarus.Application.Suporte;
using Icarus.Domain.Entities;
using Icarus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Suporte;

internal sealed class SuporteService(IcarusDbContext banco, TrelloSuporte trello, TimeProvider relogio) : ISuporte
{
    public async Task<ResumoRelato> EnviarAsync(Guid usuarioId, string chaveEnvio, string assunto, string descricao,
        CancellationToken cancelamento)
    {
        await banco.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO relatos_suporte (usuario_id, chave_envio, referencia_cartao, assunto, descricao, criado_em)
            VALUES ({usuarioId}, {chaveEnvio}, {Guid.NewGuid()}, {assunto}, {descricao}, {relogio.GetUtcNow()})
            ON CONFLICT (usuario_id, chave_envio) DO NOTHING
            """, cancelamento);

        await using var transacao = await banco.Database.BeginTransactionAsync(cancelamento);
        var encontrados = await banco.Set<RelatoSuporte>().FromSqlInterpolated($"""
            SELECT * FROM relatos_suporte
            WHERE usuario_id = {usuarioId} AND chave_envio = {chaveEnvio}
            FOR UPDATE
            """).ToListAsync(cancelamento);
        var relato = encontrados.Single();
        if (relato.Assunto != assunto || relato.Descricao != descricao)
            throw new ChaveEnvioEmUsoException();

        if (relato.CartaoId is not null)
        {
            await transacao.CommitAsync(cancelamento);
            return Resumo(relato, StatusRelato.Recebido);
        }

        var referencia = $"Referência Icarus: {relato.ReferenciaCartao:N}";
        var cartoes = await trello.ListarAsync(incluirDescricao: true, cancelamento);
        var cartao = Array.Find(cartoes, c => c.Desc?.StartsWith(referencia + "\n", StringComparison.Ordinal) is true);

        if (cartao is null)
        {
            var agora = relogio.GetUtcNow();
            // Depois de um timeout, dê tempo para o Trello concluir uma eventual criação em andamento.
            if (relato.EnvioTentadoEm is { } tentativa && agora - tentativa < TimeSpan.FromSeconds(20))
                throw new SuporteIndisponivelException();

            relato.EnvioTentadoEm = agora;
            await banco.SaveChangesAsync(cancelamento);
            try
            {
                // O trecho remoto é limitado pelo timeout do cliente. Uma desconexão do app
                // não deve impedir que o servidor guarde o ID do cartão já criado.
                cartao = await trello.CriarAsync($"[{relato.Protocolo}] {assunto}", $"{referencia}\n\n{descricao}", CancellationToken.None);
            }
            catch (SuporteIndisponivelException)
            {
                await transacao.CommitAsync(CancellationToken.None);
                throw;
            }
        }

        relato.CartaoId = cartao.Id;
        await banco.SaveChangesAsync(CancellationToken.None);
        await transacao.CommitAsync(CancellationToken.None);
        return Resumo(relato, trello.Status(cartao.IdList));
    }

    public async Task<IReadOnlyList<ResumoRelato>> ListarAsync(Guid usuarioId, CancellationToken cancelamento)
    {
        var relatos = await RelatosEnviados(usuarioId)
            .OrderByDescending(r => r.CriadoEm).ThenByDescending(r => r.Id).ToListAsync(cancelamento);
        if (relatos.Count == 0) return [];

        var cartoes = await trello.ListarAsync(incluirDescricao: false, cancelamento);
        var listaPorCartao = cartoes.ToDictionary(c => c.Id, c => c.IdList);
        return relatos.Select(r => Resumo(r, trello.Status(listaPorCartao.GetValueOrDefault(r.CartaoId!)))).ToList();
    }

    public async Task<DetalheRelato?> ConsultarAsync(Guid usuarioId, long id, CancellationToken cancelamento)
    {
        var relato = await RelatosEnviados(usuarioId).SingleOrDefaultAsync(r => r.Id == id, cancelamento);
        if (relato is null) return null;

        var cartao = await trello.ConsultarAsync(relato.CartaoId!, cancelamento);
        var historico = new List<EtapaRelato> { new(StatusRelato.Recebido, relato.CriadoEm) };
        if (cartao is not null)
        {
            var mudancas = await trello.HistoricoAsync(cartao.Id, cancelamento);
            foreach (var acao in mudancas.DistinctBy(m => m.Id).OrderBy(m => m.Date).ThenBy(m => m.Id, StringComparer.Ordinal))
            {
                var status = trello.Status(acao.Data?.ListAfter?.Id);
                if (status != StatusRelato.Indisponivel && status != historico[^1].Status)
                    historico.Add(new(status, acao.Date));
            }
        }

        return new(relato.Id, relato.Protocolo, relato.Assunto, trello.Status(cartao?.IdList), relato.Descricao, historico);
    }

    private IQueryable<RelatoSuporte> RelatosEnviados(Guid usuarioId) =>
        banco.Set<RelatoSuporte>().AsNoTracking().Where(r => r.UsuarioId == usuarioId && r.CartaoId != null);

    private static ResumoRelato Resumo(RelatoSuporte relato, StatusRelato status) =>
        new(relato.Id, relato.Protocolo, relato.Assunto, status);
}
