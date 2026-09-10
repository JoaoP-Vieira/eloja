using eloja_ordem_worker.Infrastructure;
using eloja_ordem_worker.Model;

namespace eloja_ordem_worker.Commands
{
	public record OrdemCanceladaCommand(Guid ordemId, string motivoCancelamento);

	public class OrdemCanceladaCommandHandler
	{
		private readonly ILogger<OrdemCanceladaCommandHandler> _logger;
		private readonly IOrdemStore _ordemStore;

		public OrdemCanceladaCommandHandler(ILogger<OrdemCanceladaCommandHandler> logger, IOrdemStore ordemStore)
		{
			_logger = logger;
			_ordemStore = ordemStore;
		}

		public async Task Handle(OrdemCanceladaCommand command)
		{
			try
			{
				var ordem = await _ordemStore.GetAsync(command.ordemId.ToString());

				if (ordem == null)
				{
					_logger.LogWarning("Ordem {ordemId} não encontrada", command.ordemId);

					throw new Exception($"Ordem {command.ordemId} não encontrada");
				}

				ordem.Status = OrdemStatus.Cancelada;
				ordem.MotivoCancelamento = command.motivoCancelamento;

				await _ordemStore.UpdateAsync(ordem);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao cancelar ordem {ordemId}", command.ordemId);
				throw;
			}
		}
	}
}
