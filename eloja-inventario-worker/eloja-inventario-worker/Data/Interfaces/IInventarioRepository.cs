using eloja_inventario_worker.Model;
using Npgsql;

namespace eloja_inventario_worker.Data.Interfaces
{
	public interface IInventarioRepository
	{
		Task<Inventario?> SelectByIdProduto(Guid produtoId);
		Task SaveReservaInventarioAsync(Produto produto);
	}
}
