using System.Text.Json.Serialization;

namespace Icarus.Api.Contracts.Campanhas;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrarProgressoCampanhaRequest(decimal? ValorAtual, string VersaoEsperada, Guid OperacaoId);
