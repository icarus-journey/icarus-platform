using System.Data;
using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class ReferenciasPublicasRepository(IcarusDbContext contexto) : IReferenciasPublicasRepository
{
    private static readonly JsonSerializerOptions Opcoes = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<SnapshotReferenciasPublicas?> ObterVigenteAsync(string areaDaVida, CancellationToken ct)
    {
        var conexao = contexto.Database.GetDbConnection();
        if (conexao.State != ConnectionState.Open) await conexao.OpenAsync(ct);
        await using var comando = conexao.CreateCommand();
        // Uma instrução lê ponteiro, metadados e referências sob o mesmo snapshot MVCC.
        comando.CommandText = """
            SELECT s.checksum, s.metadados::text,
                COALESCE((SELECT jsonb_agg(r.conteudo ORDER BY r.id)
                    FROM dados_publicos.referencias r WHERE r.versao_gold=s.versao_gold
                    AND r.conteudo->>'area_da_vida'=@area), '[]'::jsonb)::text
            FROM dados_publicos.snapshot_vigente v
            JOIN dados_publicos.snapshots s ON s.versao_gold=v.versao_gold WHERE v.chave=true
            """;
        var parametro = comando.CreateParameter(); parametro.ParameterName = "area"; parametro.Value = areaDaVida;
        comando.Parameters.Add(parametro);
        try
        {
            await using var leitor = await comando.ExecuteReaderAsync(ct);
            if (!await leitor.ReadAsync(ct)) return null;
            var metadados = JsonSerializer.Deserialize<MetadadosPublicos>(leitor.GetString(1), Opcoes)
                ?? throw new JsonException("Metadados ausentes.");
            var dados = JsonSerializer.Deserialize<List<ReferenciaPublica>>(leitor.GetString(2), Opcoes)
                ?? throw new JsonException("Referências ausentes.");
            return new(leitor.GetString(0), metadados, dados);
        }
        catch (PostgresException erro) when (erro.SqlState is "42P01" or "3F000") { return null; }
    }
}
