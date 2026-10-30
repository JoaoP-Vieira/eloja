namespace eloja.Data.Entities
{
	public class ProdutoListagem
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descricao { get; set; } = string.Empty;
		public decimal PrecoUnitario { get; set; }
		public Guid? CategoriaId { get; set; }
		public string? CategoriaNome { get; set; }
		public string? CategoriaDescricao { get; set; }
		public bool TemEstoque { get; set; }
	}
}
