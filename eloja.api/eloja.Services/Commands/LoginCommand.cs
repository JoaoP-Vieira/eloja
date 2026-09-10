using eloja.Data.Interface;
using eloja.Services.Auth;
using eloja.Services.DTO;

namespace eloja.Services.Commands
{
	public record LoginCommand(string email, string password);
	public class LoginCommandHandler(
		IUsuarioRepository _userRepository,
		IPasswordHasher _passwordHasher,
		IJwtTokenGenerator _jwtTokenGenerator)
	{
		public async Task<AuthResultDTO> Handle(LoginCommand request, CancellationToken ct = default)
		{
			var user = await _userRepository.SelectByEmailAsync(request.email);

			if (user == null || !_passwordHasher.Verify(request.password, user.PasswordHash))
				throw new UnauthorizedAccessException("Usuário ou senha inválidos");

			var tokenResult = _jwtTokenGenerator.GenerateToken(user);

			return new AuthResultDTO
			{
				Token = tokenResult.Token,
				ExpiresAtUtc = tokenResult.ExpiresAtUtc,
				Usuario = new UsuarioDTO
				{
					Id = user.Id,
					Email = user.Email,
					Nome = user.Nome,
					Sobrenome = user.Sobrenome
				}
			};
		}
	}
}
