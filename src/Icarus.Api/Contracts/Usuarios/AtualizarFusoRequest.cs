using System.Text.Json.Serialization;

namespace Icarus.Api.Contracts.Usuarios;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AtualizarFusoRequest(string FusoHorario);

public sealed record AtualizarFusoResponse(Guid Id, string FusoHorario);
