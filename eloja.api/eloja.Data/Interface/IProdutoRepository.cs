using eloja.Data.Entities;

namespace eloja.Data.Interface
{
	public interface IProdutoRepository
	{
		Task<Produto?> SelectByIdAsync(Guid id);
		Task<bool> ExistsByIdAsync(Guid id);
		Task InsertAsync(Produto produto);
		Task<IEnumerable<ProdutoListagem>> SelectPaginadoAsync(FiltroProdutoListagem filtro);
		Task<int> CountAsync(FiltroProdutoListagem filtro);
	}
}
