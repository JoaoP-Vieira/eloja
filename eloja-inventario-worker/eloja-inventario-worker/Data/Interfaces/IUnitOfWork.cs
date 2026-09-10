namespace eloja_inventario_worker.Data.Interfaces
{
	public interface IUnitOfWork
	{
		Task BeginTransactionAsync();
		Task CommitAsync();
		Task RollbackAsync();
	}
}
