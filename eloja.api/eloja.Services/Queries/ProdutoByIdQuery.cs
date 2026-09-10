using eloja.Data.Interface;
using eloja.Services.DTO;

namespace eloja.Services.Queries
{
	public record ProdutoByIdQuery(Guid Id);

	public class ProdutoByIdQueryHandler(IProdutoRepository _produtoRepository, ICategoriaRepository _categoriaRepository)
	{
		public async Task<ProdutoDTO> Handle(ProdutoByIdQuery request, CancellationToken ct = default)
		{
			var produto = await _produtoRepository.SelectByIdAsync(request.Id);
			
			if (produto == null)
				throw new KeyNotFoundException("Produto não encontrado");

			var categoria = await _categoriaRepository.SelectByIdAsync(produto.CategoriaId);
			
			return ProdutoDTO.FromEntity(produto, categoria);
		}
	}
}
