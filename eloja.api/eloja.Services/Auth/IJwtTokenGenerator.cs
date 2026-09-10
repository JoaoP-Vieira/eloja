using eloja.Data.Entities;

namespace eloja.Services.Auth
{
	public interface IJwtTokenGenerator
	{
		JwtTokenResult GenerateToken(Usuario usuario);
	}

	public record JwtTokenResult(string Token, DateTime ExpiresAtUtc);
}
