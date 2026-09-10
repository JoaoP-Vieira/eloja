using eloja_inventario_worker.Data.Interfaces;
using eloja_inventario_worker.Model;
using Wolverine;

namespace eloja_inventario_worker.Commands
{
	public record ReservaInventarioDbSaveCommand(Guid ordemId, int usuarioId, Produto[] produtos);
		
	public class ReservaInventarioDbSaveCommandHandler 
	{
		private readonly ILogger<ReservaInventarioDbSaveCommandHandler> _logger;
		private readonly IInventarioRepository _inventarioRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMessageBus _bus;

		public ReservaInventarioDbSaveCommandHandler(
			ILogger<ReservaInventarioDbSaveCommandHandler> logger,
			IInventarioRepository inventarioRepository,
			IUnitOfWork unitOfWork,
			IMessageBus bus)
		{
			_logger = logger;
			_inventarioRepository = inventarioRepository;
			_unitOfWork = unitOfWork;
			_bus = bus;
		}

		public async Task Handle(ReservaInventarioDbSaveCommand command)
		{
			await _unitOfWork.BeginTransactionAsync();

			foreach (var produto in command.produtos)
			{
				try
				{
					await _inventarioRepository.SaveReservaInventarioAsync(produto);

					await _bus.PublishAsync(new OrdemConfirmadaCommand(command.ordemId));
				}
				catch (Exception ex)
				{
					await _unitOfWork.RollbackAsync();

					_logger.LogError(ex, "Erro ao salvar reserva de inventário no banco de dados.");

					throw;
				}
			}

			await _unitOfWork.CommitAsync();
		}
	}
}
