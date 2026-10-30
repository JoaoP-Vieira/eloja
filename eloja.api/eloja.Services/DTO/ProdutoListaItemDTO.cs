using eloja.Data.Entities;

namespace eloja.Services.DTO
{
	public class ProdutoListaItemDTO : ProdutoDTO
	{
		public bool TemEstoque { get; set; }

		public static ProdutoListaItemDTO FromListagem(ProdutoListagem produto)
		{
			return new ProdutoListaItemDTO
			{
				Id = produto.Id,
				Nome = produto.Nome,
				Descricao = produto.Descricao,
				PrecoUnitario = produto.PrecoUnitario,
				Categoria = produto.CategoriaId == null
					? new CategoriaDTO()
					: new CategoriaDTO
					{
						Id = produto.CategoriaId.Value,
						Nome = produto.CategoriaNome ?? string.Empty,
						Descricao = produto.CategoriaDescricao ?? string.Empty
					},
				TemEstoque = produto.TemEstoque
			};
		}
	}
}
