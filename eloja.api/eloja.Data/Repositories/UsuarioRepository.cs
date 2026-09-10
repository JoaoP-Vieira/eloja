using Dapper;
using eloja.Data.Entities;
using eloja.Data.Interface;

namespace eloja.Data.Repositories
{
	public class UsuarioRepository : BaseRepository, IUsuarioRepository
	{
		private const string INSERT_USER = @"INSERT INTO ""usuario"" 
		(email, nome, sobrenome, password_hash) VALUES (@Email, @Nome, @Sobrenome, @PasswordHash)";

		private const string SELECT_BY_EMAIL = @"SELECT id AS ""Id"", email AS ""Email"", nome AS ""Nome"",
			sobrenome AS ""Sobrenome"", password_hash AS ""PasswordHash""
		FROM ""usuario"" WHERE email = @Email";

		private const string GET_LAST_ID = "SELECT currval(pg_get_serial_sequence('usuario','id'))";

		public UsuarioRepository(IPgDbContext dbContext) : base(dbContext) { }

		public async Task<int> InsertAsync(Usuario usuario)
		{
			var conn = _dbContext.GetConnection();
			var transaction = _dbContext.Transaction;

			await conn.ExecuteAsync(INSERT_USER,
						   new { usuario.Email, usuario.Nome, usuario.Sobrenome, usuario.PasswordHash },
						   transaction);

			var result = await conn.QueryAsync<int>(GET_LAST_ID);

			return result.FirstOrDefault();
		}

		public async Task<Usuario?> SelectByEmailAsync(string email)
		{
			var conn = _dbContext.GetConnection();

			var result = await conn.QueryFirstOrDefaultAsync<Usuario>(SELECT_BY_EMAIL, new { Email = email });

			return result;
		}
	}
}
