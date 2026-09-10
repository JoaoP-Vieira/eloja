using eloja.Data.Entities;

namespace eloja.Services.DTO
{
	public class ProdutoDTO
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descricao { get; set; } = string.Empty;
		public CategoriaDTO Categoria { get; set; } = new CategoriaDTO();
		public decimal PrecoUnitario { get; set; }

		public static ProdutoDTO FromEntity(Produto produto, Categoria? categoria)
		{
			return new ProdutoDTO
			{
				Id = produto.Id,
				Nome = produto.Nome,
				Descricao = produto.Descricao,
				Categoria = categoria == null ? new CategoriaDTO() : CategoriaDTO.FromEntity(categoria),
				PrecoUnitario = produto.PrecoUnitario
			};
		}
	}
}
