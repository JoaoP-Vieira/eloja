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

		// Leitura somente de public.inventario (tabela do inventario-worker); saldo = disponivel - reservada
		private const string LISTAGEM_FROM_WHERE = @"from
			public.produto p
			left join public.categoria c on c.id = p.categoria_id
			left join (
				select produto_id, sum(quantidade_disponivel - quantidade_reservada) saldo
				from public.inventario
				group by produto_id
			) i on i.produto_id = p.id
		where
			(@nome::text is null or position(lower(@nome::text) in lower(p.nome)) > 0)
			and (@categoriaId::uuid is null or p.categoria_id = @categoriaId::uuid)";

		private const string LISTAGEM_SELECT = @"select
			p.id ""Id"",
			p.nome ""Nome"",
			p.descricao ""Descricao"",
			p.preco_unitario ""PrecoUnitario"",
			p.categoria_id ""CategoriaId"",
			c.nome ""CategoriaNome"",
			c.descricao ""CategoriaDescricao"",
			coalesce(i.saldo, 0) > 0 ""TemEstoque"" ";

		private const string LISTAGEM_PAGINACAO = " limit @tamanhoPagina offset (@pagina - 1) * @tamanhoPagina";

		private const string LISTAGEM_ORDER_ASC = " order by (coalesce(i.saldo, 0) > 0) desc, lower(p.nome) asc, p.id ";
		private const string LISTAGEM_ORDER_DESC = " order by (coalesce(i.saldo, 0) > 0) desc, lower(p.nome) desc, p.id ";

		private const string COUNT_QUERY = "select count(*) " + LISTAGEM_FROM_WHERE;

		public ProdutoRepository(IPgDbContext dbContext) : base(dbContext) { }

		public async Task<IEnumerable<ProdutoListagem>> SelectPaginadoAsync(FiltroProdutoListagem filtro)
		{
			var conn = _dbContext.GetConnection();

			var order = filtro.Descendente ? LISTAGEM_ORDER_DESC : LISTAGEM_ORDER_ASC;
			var sql = LISTAGEM_SELECT + LISTAGEM_FROM_WHERE + order + LISTAGEM_PAGINACAO;

			return await conn.QueryAsync<ProdutoListagem>(sql, ParametrosListagem(filtro));
		}

		public async Task<int> CountAsync(FiltroProdutoListagem filtro)
		{
			var conn = _dbContext.GetConnection();

			return await conn.ExecuteScalarAsync<int>(COUNT_QUERY, ParametrosListagem(filtro));
		}

		private static object ParametrosListagem(FiltroProdutoListagem filtro) => new
		{
			nome = filtro.Nome,
			categoriaId = filtro.CategoriaId,
			pagina = filtro.Pagina,
			tamanhoPagina = filtro.TamanhoPagina
		};

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
