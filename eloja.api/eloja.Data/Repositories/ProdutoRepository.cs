using Dapper;
using eloja.Data.Entities;
using eloja.Data.Interface;

namespace eloja.Data.Repositories
{
	public class ProdutoRepository : BaseRepository, IProdutoRepository
	{
		private const string SELECT_BY_ID_QUERY = @"select
			p.id ""Id"",
			p.nome ""Nome"",
			p.descricao ""Descricao"",
			p.foto ""Foto"",
			p.categoria_id  ""CategoriaId"",
			p.criado_em ""CriadoEm"",
			p.modificado_em ""ModificadoEm"",
			p.preco_unitario ""PrecoUnitario""
		from
			public.produto p WHERE p.id = @id";

		private const string EXISTS_BY_ID_QUERY = @"select 1 ""Result"" from public.produto p WHERE p.id = @id";

		private const string INSERT_QUERY = @"insert into public.produto (
			id,
			nome,
			descricao,
			foto,
			categoria_id,
			criado_em,
			modificado_em,
			preco_unitario)
		values (
			@Id,
			@Nome,
			@Descricao,
			@Foto,
			@CategoriaId,
			@CriadoEm,
			NULL,
			@PrecoUnitario)";

		public ProdutoRepository(IPgDbContext dbContext) : base(dbContext) { }

		public async Task<Produto?> SelectByIdAsync(Guid id)
		{
			var conn = _dbContext.GetConnection();

			var result = await conn.QueryAsync<Produto>(SELECT_BY_ID_QUERY, new { id });

			return result.FirstOrDefault();
		}

		public async Task<bool> ExistsByIdAsync(Guid id)
		{
			var conn = _dbContext.GetConnection();

			var result = await conn.QueryAsync(EXISTS_BY_ID_QUERY, new { id });

			return result != null && result.Any() && result.First().Result == 1;
		}

		public async Task InsertAsync(Produto produto)
		{
			var conn = _dbContext.GetConnection();

			await conn.ExecuteAsync(INSERT_QUERY, produto, _dbContext.Transaction);
		}
	}
}
