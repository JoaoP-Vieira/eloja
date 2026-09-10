namespace eloja.Services.DTO
{
	public class AuthResultDTO
	{
		public string Token { get; set; } = string.Empty;
		public DateTime ExpiresAtUtc { get; set; }
		public UsuarioDTO Usuario { get; set; } = new UsuarioDTO();
	}
}
