using eloja.Data.Interface;

namespace eloja.Data
{
	public class UnitOfWork : IUnitOfWork, IDisposable
	{
		private readonly IPgDbContext _dbContext;

		public UnitOfWork(IPgDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task BeginTransactionAsync()
		{
			await _dbContext.BeginTransactionAsync();
		}

		public async Task CommitAsync()
		{
			if (_dbContext.Transaction != null)
			{
				await _dbContext.Transaction.CommitAsync();
			}
		}

		public async Task RollbackAsync()
		{
			if (_dbContext.Transaction != null)
			{
				await _dbContext.Transaction.RollbackAsync();

				if (_dbContext.Transaction.Connection != null)
					await _dbContext.Transaction.Connection.CloseAsync();
			}
		}

		public void Dispose()
		{
			_dbContext.Transaction?.Dispose();
		}
	}
}
