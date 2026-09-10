using eloja.Data.Entities;

namespace eloja.Services.DTO
{
	public class CategoriaDTO
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descricao { get; set; } = string.Empty;

		public static CategoriaDTO FromEntity(Categoria categoria)
		{
			return new CategoriaDTO
			{
				Id = categoria.Id,
				Nome = categoria.Nome,
				Descricao = categoria.Descricao
			};
		}
	}
}
