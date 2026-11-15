using System.Text.Json.Serialization;

namespace Icarus.Api.Contracts.Usuarios;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AtualizarRelatoSonoRequest(int? HabitualMinutos, int? MetaMinutos, Guid Versao);
