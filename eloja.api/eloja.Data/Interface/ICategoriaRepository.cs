using eloja.Data.Entities;

namespace eloja.Data.Interface
{
	public interface ICategoriaRepository
	{
		Task<Categoria?> SelectByIdAsync(Guid id);
	}
}
