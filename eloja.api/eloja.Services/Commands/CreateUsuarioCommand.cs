using eloja.Data.Entities;
using eloja.Data.Interface;
using eloja.Services.Auth;
using eloja.Services.DTO;

namespace eloja.Services.Commands
{
	public record CreateUsuarioCommand(string email, string nome, string sobrenome, string password);

	public class CreateUsuarioCommandHandler(
		IUsuarioRepository _usuarioRepository,
		IPasswordHasher _passwordHasher
	)
	{
		public async Task<UsuarioDTO> Handle(CreateUsuarioCommand request, CancellationToken ct = default)
		{
			var existingUser = await _usuarioRepository.SelectByEmailAsync(request.email);

			if (existingUser != null)
				throw new ArgumentException("E-mail já em uso", nameof(request.email));

			var passwordHash = _passwordHasher.Hash(request.password);

			var user = new Usuario(request.email, request.nome, request.sobrenome, passwordHash);

			var id = await _usuarioRepository.InsertAsync(user);

			return new UsuarioDTO
			{
				Id = id, Email = user.Email, Nome = user.Nome, Sobrenome = user.Sobrenome
			};
		}
	}
}
