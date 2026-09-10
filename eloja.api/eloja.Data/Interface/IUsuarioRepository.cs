using eloja.Data.Entities;

namespace eloja.Data.Interface
{
	public interface IUsuarioRepository
	{
		Task<int> InsertAsync(Usuario usuario);
		Task<Usuario?> SelectByEmailAsync(string email);
	}
}
