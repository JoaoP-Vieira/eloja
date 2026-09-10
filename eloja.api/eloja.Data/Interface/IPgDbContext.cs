using Npgsql;

namespace eloja.Data.Interface
{
	public interface IPgDbContext
	{
		NpgsqlConnection GetConnection();
		NpgsqlTransaction BeginTransaction();
		NpgsqlTransaction? Transaction { get; }
		Task<NpgsqlTransaction> BeginTransactionAsync();
	}
}
