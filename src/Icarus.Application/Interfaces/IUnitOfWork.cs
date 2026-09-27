namespace Icarus.Application.Interfaces;

/// <summary>
/// Confirma (commita) as mudanças feitas por um ou mais repositórios na mesma
/// transação. Separado dos repositórios porque alguns fluxos (ex.: concluir
/// uma ocorrência e lançar a movimentação de pontos correspondente) precisam
/// gravar mais de uma entidade atomicamente — ver seção 8.3 da arquitetura no
/// icarus-context.
/// </summary>
public interface IUnitOfWork
{
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
