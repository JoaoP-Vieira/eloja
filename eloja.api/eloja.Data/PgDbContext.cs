using eloja.Data.Interface;
using Npgsql;
using System.Data;

namespace eloja.Data
{
	public class PgDbContext : IPgDbContext, IDisposable, IAsyncDisposable
	{
		private readonly string _connStrig;
		private NpgsqlConnection? _connection;

		public NpgsqlTransaction? Transaction { get; private set; }

		public PgDbContext(string connStrig)
		{
			_connStrig = connStrig;
		}

		public NpgsqlConnection GetConnection()
		{
			if (_connection == null)
			{
				_connection = new NpgsqlConnection(_connStrig);
			}

			if (_connection.State != ConnectionState.Open)
			{
				_connection.Open();
			}

			return _connection;
		}

		public NpgsqlTransaction BeginTransaction()
		{
			var connection = GetConnection();
			Transaction = connection.BeginTransaction();
			return Transaction;
		}

		public async Task<NpgsqlTransaction> BeginTransactionAsync()
		{
			var connection = GetConnection();
			Transaction = await connection.BeginTransactionAsync();
			return Transaction;
		}

		public void Dispose()
		{
			if (_connection != null && _connection.State != ConnectionState.Closed)
			{
				_connection.Dispose();
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (_connection != null && _connection.State != ConnectionState.Closed)
			{
				await _connection.DisposeAsync();
			}
		}
	}
}
