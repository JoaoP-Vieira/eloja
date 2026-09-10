using eloja_inventario_worker.Data.Interfaces;

namespace eloja_inventario_worker.Data
{
	public class BaseRepository
	{
		protected readonly IPgDbContext _dbContext;

		protected BaseRepository(IPgDbContext dbContext)
		{
			_dbContext = dbContext;
		}
	}
}
