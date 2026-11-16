using System.Text.Json.Serialization;

namespace Icarus.Api.Contracts.Usuarios;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AtualizarPerfilRequest(bool? Estuda, decimal? CargaHorariaEstudoSemanal,
    bool? Trabalha, decimal? CargaHorariaTrabalhoSemanal);
