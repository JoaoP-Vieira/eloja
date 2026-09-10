using eloja_ordem_worker.Infrastructure;
using eloja_ordem_worker.Model;

namespace eloja_ordem_worker.Commands
{
	public record OrdemCriadaCommand(Guid ordemId, int usuarioId, Produto[] Produtos);

	public class OrdemCriadaCommandHandler
	{
		private readonly ILogger<OrdemCriadaCommandHandler> _logger;
		private readonly IOrdemStore _ordemStore;

		public OrdemCriadaCommandHandler(ILogger<OrdemCriadaCommandHandler> logger, IOrdemStore ordemStore)
		{
			_logger = logger;
			_ordemStore = ordemStore;
		}

		public async Task Handle(OrdemCriadaCommand command)
		{
			try
			{
				await _ordemStore.CreateAsync(new Ordem(command.ordemId.ToString(), command.usuarioId, command.Produtos.ToList()));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao criar ordem {ordemId}", command.ordemId);

				throw;
			}
		}
	}
}
