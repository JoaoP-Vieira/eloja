using Dapper;
using eloja_inventario_worker.Data.Interfaces;
using eloja_inventario_worker.Model;
using Polly.Registry;

namespace eloja_inventario_worker.Data
{
	public class InventarioRepository : BaseRepository, IInventarioRepository
	{
		const string SELECT_BASE = @"SELECT
			id,
			produto_id,
			quantidade_disponivel,
			quantidade_reservada,
			criado_em,
			modificado_em
		FROM inventario";

		const string SELECT_BY_ID_PRODUTO = SELECT_BASE + " WHERE produto_id = @produtoId";

		const string UPDATE_RESERVA_INVENTARIO = @"UPDATE inventario
			SET quantidade_reservada = quantidade_reservada + @quantidade,
				modificado_em = NOW()
			WHERE produto_id = @produtoId";

		public InventarioRepository(IPgDbContext dbContext, ResiliencePipelineProvider<string> pipelineProvider)
			: base(dbContext, pipelineProvider) { }

		public async Task<Inventario?> SelectByIdProduto(Guid produtoId)
		{
			var conn = _dbContext.GetConnection();

			var result = await conn.QueryAsync<Inventario>(SELECT_BY_ID_PRODUTO, new { produtoId });

			return result.FirstOrDefault();
		}

		public async Task SaveReservaInventarioAsync(Produto produto)
		{
			var conn = _dbContext.GetConnection();
			var transaction = _dbContext.Transaction;

			await _pipeline.ExecuteAsync(async (ctx) =>
				await conn.ExecuteAsync(UPDATE_RESERVA_INVENTARIO,
					new {
						produtoId = produto.id,
						quantidade = produto.quantidadeSolicitada
					}, transaction));
		}
	}
}
