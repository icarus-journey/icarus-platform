namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada ao tentar editar uma missão recorrente — ela nunca é editável, diferente da não
/// recorrente (que só fica imutável depois de concluída). Para mudar algo, o usuário para a
/// missão (RF-04.6) e cria uma nova.
/// </summary>
public sealed class MissaoRecorrenteImutavelException()
    : Exception("Missão recorrente não pode ser editada. Pare esta missão e crie uma nova para alterar algo.");
