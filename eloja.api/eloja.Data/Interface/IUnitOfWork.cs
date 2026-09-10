namespace eloja.Data.Interface
{
	public interface IUnitOfWork
	{
		Task BeginTransactionAsync();
		Task CommitAsync();
		Task RollbackAsync();
	}
}
