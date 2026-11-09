using System.Security.Cryptography;
using System.Text.Json;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using DadosCursor = Icarus.Application.UseCases.ConsultarExtratoPontos.CursorExtratoPontos;

namespace Icarus.Api.Contracts.ExtratoPontos;

public sealed class CursorExtratoPontos(IDataProtectionProvider protecao) : ICursorExtratoPontos
{
    private readonly IDataProtector _protetor = protecao.CreateProtector("Icarus.ExtratoPontos.v1");
    public string Codificar(DadosCursor cursor) => _protetor.Protect(JsonSerializer.Serialize(cursor));
    public DadosCursor Decodificar(string cursor)
    {
        try
        {
            if (cursor.Length > 4096) throw new ConsultaExtratoInvalidaException();
            var dados = JsonSerializer.Deserialize<DadosCursor>(_protetor.Unprotect(cursor));
            return dados?.Filtro is not null ? dados : throw new ConsultaExtratoInvalidaException();
        }
        catch (Exception erro) when (erro is CryptographicException or JsonException or FormatException or ArgumentException)
        { throw new ConsultaExtratoInvalidaException(); }
    }
}
