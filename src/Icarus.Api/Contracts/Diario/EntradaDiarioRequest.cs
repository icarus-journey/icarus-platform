using System.Text.Json.Serialization;
using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Diario;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EntradaDiarioRequest(TipoEntradaDiario? Tipo, DateOnly DataRegistro, string Texto);
