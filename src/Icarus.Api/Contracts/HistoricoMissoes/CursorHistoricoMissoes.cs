using System.Security.Cryptography;
using System.Text.Json;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using CursorDados = Icarus.Application.UseCases.ConsultarHistoricoMissoes.CursorHistoricoMissoes;
using Microsoft.AspNetCore.DataProtection;

namespace Icarus.Api.Contracts.HistoricoMissoes;

public sealed class CursorHistoricoMissoes(IDataProtectionProvider protecao) : ICursorHistoricoMissoes
{
    private readonly IDataProtector protetor = protecao.CreateProtector("Icarus.HistoricoMissoes.v1");
    public string Codificar(CursorDados cursor)
        => protetor.Protect(JsonSerializer.Serialize(cursor));
    public CursorDados Decodificar(string cursor)
    {
        try
        {
            if (cursor.Length > 4096) throw new ConsultaHistoricoInvalidaException();
            var valor = JsonSerializer.Deserialize<CursorDados>(protetor.Unprotect(cursor));
            return valor?.Filtro is not null ? valor : throw new ConsultaHistoricoInvalidaException();
        }
        catch (Exception erro) when (erro is CryptographicException or JsonException or FormatException or ArgumentException)
        { throw new ConsultaHistoricoInvalidaException(); }
    }
}
