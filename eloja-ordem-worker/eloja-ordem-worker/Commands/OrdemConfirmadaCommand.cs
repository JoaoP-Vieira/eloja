using eloja_ordem_worker.Infrastructure;
using eloja_ordem_worker.Model;

namespace eloja_ordem_worker.Commands
{
	public record OrdemConfirmadaCommand(Guid ordemId);

	public class OrdemConfirmadaCommandHandler
	{
		private readonly ILogger<OrdemConfirmadaCommandHandler> _logger;
		private readonly IOrdemStore _ordemStore;

		public OrdemConfirmadaCommandHandler(ILogger<OrdemConfirmadaCommandHandler> logger, IOrdemStore ordemStore)
		{
			_logger = logger;
			_ordemStore = ordemStore;
		}

		public async Task Handle(OrdemConfirmadaCommand command)
		{
			try
			{
				var ordem = await _ordemStore.GetAsync(command.ordemId.ToString());

				if (ordem == null)
				{
					_logger.LogWarning("Ordem {ordemId} não encontrada", command.ordemId);

					throw new Exception($"Ordem {command.ordemId} não encontrada");
				}

				ordem.Status = OrdemStatus.Confirmada;

				await _ordemStore.UpdateAsync(ordem);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao confirmar ordem {ordemId}", command.ordemId);
				throw;
			}
		}
	}
}
