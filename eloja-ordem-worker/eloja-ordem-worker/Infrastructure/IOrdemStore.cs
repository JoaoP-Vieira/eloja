using eloja_ordem_worker.Model;

namespace eloja_ordem_worker.Infrastructure
{
	public interface IOrdemStore
	{
		Task<Ordem?> GetAsync(string id);
		Task<List<Ordem>> GetByUsuarioIdAsync(int usuarioId);
		Task CreateAsync(Ordem ordem);
		Task UpdateAsync(Ordem ordem);
	}
}
