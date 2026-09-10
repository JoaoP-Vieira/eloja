using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using eloja.Data.Entities;

namespace eloja.Services.Auth
{
	public class JwtTokenGenerator : IJwtTokenGenerator
	{
		private readonly JwtSettings _settings;

		public JwtTokenGenerator(IOptions<JwtSettings> options)
		{
			_settings = options.Value;
		}

		public JwtTokenResult GenerateToken(Usuario usuario)
		{
			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
				new Claim(JwtRegisteredClaimNames.GivenName, usuario.Nome),
				new Claim(JwtRegisteredClaimNames.FamilyName, usuario.Sobrenome),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
			var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

			var token = new JwtSecurityToken(
				issuer: _settings.Issuer,
				audience: _settings.Audience,
				claims: claims,
				expires: expiresAtUtc,
				signingCredentials: credentials);

			var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

			return new JwtTokenResult(tokenString, expiresAtUtc);
		}
	}
}
