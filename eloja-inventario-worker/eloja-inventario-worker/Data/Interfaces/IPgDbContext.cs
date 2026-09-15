using Npgsql;
using Polly;

namespace eloja_inventario_worker.Data.Interfaces
{
	public interface IPgDbContext
	{
		NpgsqlConnection GetConnection();
		NpgsqlTransaction BeginTransaction();
		NpgsqlTransaction? Transaction { get; }
		Task<NpgsqlTransaction> BeginTransactionAsync();
	}
}
