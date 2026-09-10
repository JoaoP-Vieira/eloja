namespace eloja.Data.Entities
{
	public class Categoria
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descricao { get; set; } = string.Empty;
		public DateTime CriadoEm { get; set; }
		public DateTime? ModificadoEm { get; set; }

		public Categoria() { }

		public Categoria(string nome, string descricao)
		{
			Id = Guid.NewGuid();
			Nome = nome;
			Descricao = descricao;
			CriadoEm = DateTime.UtcNow;
			ModificadoEm = null;
		}
	}
}
