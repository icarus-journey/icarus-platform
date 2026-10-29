namespace Icarus.Application.Exceptions;

public sealed class ConfiguracaoCampanhaBloqueadaException()
    : Exception("Esta campanha já começou. Você pode continuar registrando seu progresso.");
