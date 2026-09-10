namespace eloja.Data.Entities
{
	public class Usuario
	{
		public int Id { get; set; }
		public string Email { get; set; } = string.Empty;
		public string Nome { get; set; } = string.Empty;
		public string Sobrenome { get; set; } = string.Empty;
		public string PasswordHash { get; set; } = string.Empty;

		public Usuario() { }

		public Usuario(string email, string nome, string sobrenome, string passwordHash)
		{
			Email = email;
			Nome = nome;
			Sobrenome = sobrenome;
			PasswordHash = passwordHash;
		}
	}
}
