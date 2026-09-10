namespace eloja.Data.Entities
{
	public class Produto
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descricao { get; set; } = string.Empty;
		public string? Foto { get; set; }
		public Guid CategoriaId { get; set; }
		public DateTime CriadoEm { get; set; }
		public DateTime? ModificadoEm { get; set; }
		public decimal PrecoUnitario { get; set; }

		public Produto() { }
		
		public Produto(string nome, string descricao, string? foto, Guid categoriaId, decimal precoUnitario)
		{
			if (string.IsNullOrWhiteSpace(nome))
			{
				throw new ArgumentException("Nome do produto não pode ser vazio.");
			}

			if (nome.Length > 100)
			{
				throw new ArgumentException("Nome do produto não pode ter mais de 100 caracteres.");
			}

			if (string.IsNullOrWhiteSpace(descricao))
			{
				throw new ArgumentException("Descrição do produto não pode ser vazia.");
			}

			if (descricao.Length > 500)
			{
				throw new ArgumentException("Descrição do produto não pode ter mais de 500 caracteres.");
			}

			if (precoUnitario <= 0)
			{
				throw new ArgumentException("Preço unitário do produto deve ser maior que zero.");
			}

			Id = Guid.NewGuid();
			Nome = nome;
			Descricao = descricao;
			Foto = foto;
			CategoriaId = categoriaId;
			CriadoEm = DateTime.UtcNow;
			ModificadoEm = null;
			PrecoUnitario = precoUnitario;
		}
	}
}
