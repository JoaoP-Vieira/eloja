namespace eloja.Services.DTO
{
	public class UsuarioDTO
	{
		public int Id { get; set; }
		public string Email { get; set; } = string.Empty;
		public string Nome { get; set; } = string.Empty;
		public string Sobrenome { get; set; } = string.Empty;
	}
}
