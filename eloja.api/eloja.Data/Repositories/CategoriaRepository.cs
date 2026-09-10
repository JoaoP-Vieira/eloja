using Dapper;
using eloja.Data.Entities;
using eloja.Data.Interface;

namespace eloja.Data.Repositories
{
	public class CategoriaRepository : BaseRepository, ICategoriaRepository
	{
		private const string SELECT_BY_ID_QUERY = @"select
			c.id ""Id"",
			c.nome ""Nome"",
			c.descricao ""Descricao"",
			c.criado_em ""CriadoEm"",
			c.modificado_em ""ModificadoEm""
		from
			public.categoria c WHERE c.id = @id";

		public CategoriaRepository(IPgDbContext dbContext) : base(dbContext) { }

		public async Task<Categoria?> SelectByIdAsync(Guid id)
		{
			var conn = _dbContext.GetConnection();
			
			var result = await conn.QueryAsync<Categoria>(SELECT_BY_ID_QUERY, new { id });

			return result.FirstOrDefault();
		}
	}
}
