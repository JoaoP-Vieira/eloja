using eloja_inventario_worker.Data.Interfaces;
using Polly;
using Polly.Registry;

namespace eloja_inventario_worker.Data
{
	public class UnitOfWork : IUnitOfWork, IDisposable
	{
		private readonly IPgDbContext _dbContext;
		private readonly ResiliencePipeline _pipeline;

		public UnitOfWork(IPgDbContext dbContext, ResiliencePipelineProvider<string> pipelineProvider)
		{
			_dbContext = dbContext;
			_pipeline = pipelineProvider.GetPipeline("postgres");
		}

		public async Task BeginTransactionAsync()
		{
			await _pipeline.ExecuteAsync(async (ctx)
				=> await _dbContext.BeginTransactionAsync());
		}

		public async Task CommitAsync()
		{
			if (_dbContext.Transaction != null)
			{
				await _pipeline.ExecuteAsync(async (ctx)
					=> await _dbContext.Transaction.CommitAsync());
			}
		}

		public async Task RollbackAsync()
		{
			if (_dbContext.Transaction != null)
			{
				await _pipeline.ExecuteAsync(async (ctx)
					=> await _dbContext.Transaction.RollbackAsync());

				if (_dbContext.Transaction.Connection != null)
				{
					await _pipeline.ExecuteAsync(async (ctx)
						=> await _dbContext.Transaction.Connection.CloseAsync());
				}
			}
		}

		public void Dispose()
		{
			_dbContext.Transaction?.Dispose();
		}
	}
}
