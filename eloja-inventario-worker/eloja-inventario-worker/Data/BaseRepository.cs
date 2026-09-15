using eloja_inventario_worker.Data.Interfaces;
using Polly;
using Polly.Registry;

namespace eloja_inventario_worker.Data
{
	public class BaseRepository
	{
		protected readonly IPgDbContext _dbContext;
		protected readonly ResiliencePipeline _pipeline;

		protected BaseRepository(IPgDbContext dbContext, ResiliencePipelineProvider<string> pipelineProvider)
		{
			_dbContext = dbContext;
			_pipeline = pipelineProvider.GetPipeline("postgres");
		}
	}
}
