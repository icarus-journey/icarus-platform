using System.Security.Cryptography;
using System.Text.Json;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using DadosCursor = Icarus.Application.UseCases.ConsultarHistoricoRelatorios.CursorHistoricoRelatorios;

namespace Icarus.Api.Contracts.HistoricoRelatorios;

public sealed class CursorHistoricoRelatorios(IDataProtectionProvider protecao) : ICursorHistoricoRelatorios
{
    private readonly IDataProtector _protetor = protecao.CreateProtector("Icarus.HistoricoRelatorios.v1");
    public string Codificar(DadosCursor cursor) => _protetor.Protect(JsonSerializer.Serialize(cursor));
    public DadosCursor Decodificar(string cursor)
    {
        try
        {
            if (cursor.Length > 4096) throw new ConsultaHistoricoRelatoriosInvalidaException();
            var dados = JsonSerializer.Deserialize<DadosCursor>(_protetor.Unprotect(cursor));
            return dados is not null ? dados : throw new ConsultaHistoricoRelatoriosInvalidaException();
        }
        catch (Exception erro) when (erro is CryptographicException or JsonException or FormatException or ArgumentException)
        { throw new ConsultaHistoricoRelatoriosInvalidaException(); }
    }
}
