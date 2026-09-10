using eloja_inventario_worker.Data;
using eloja_inventario_worker.Data.Interfaces;
using eloja_inventario_worker.Infrastructure;
using eloja_inventario_worker.Model;
using Prometheus;
using StackExchange.Redis;
using Wolverine;

namespace eloja_inventario_worker.Commands
{
	public record ReservarInventarioCommand(Guid ordemId, int usuarioId, Produto[] Produtos);

	public class ReservarInventarioCommandHandler
	{
		private const string ValidaEstoqueScript = @"
			local quantidadeDisponivel = redis.call('GET', KEYS[1])
			local quantidadeReservada = redis.call('GET', KEYS[2])
			
			if quantidadeDisponivel == false then
				quantidadeDisponivel = ARGV[2]

				redis.call('SET', KEYS[1], ARGV[2], 'EX', 1800)
			end

			if quantidadeReservada == false then
				quantidadeReservada = ARGV[3]

				redis.call('SET', KEYS[2], ARGV[3], 'EX', 1800)
			end

			local restante = tonumber(quantidadeDisponivel) - tonumber(quantidadeReservada) - tonumber(ARGV[1])
			
			if restante < 0 then
				return 0
			end

			redis.call('SET', KEYS[2], tonumber(quantidadeReservada) + tonumber(ARGV[1]), 'EX', 1800)

			return 1
		";

		private Inventario? RegistroInventario { get; set; }

		private readonly ILogger<ReservarInventarioCommandHandler> _logger;
		private readonly IInventarioRepository _inventarioRepository;
		private readonly IRedisConnection _redisConnection;
		private readonly IMessageBus _bus;

		private static readonly Counter ReservasProcessadas = Metrics.CreateCounter(
			"inventario_reservas_total", "Reservas processadas por resultado",
			new CounterConfiguration { LabelNames = new[] { "resultado" } });

		public ReservarInventarioCommandHandler(
			ILogger<ReservarInventarioCommandHandler> logger,
			IInventarioRepository inventarioRepository,
			IRedisConnection redisConnection,
			IMessageBus bus)
		{
			_logger = logger;
			_inventarioRepository = inventarioRepository;
			_redisConnection = redisConnection;
			_bus = bus;
		}

		public async Task Handle(ReservarInventarioCommand command, CancellationToken cancellationToken)
		{
			if (command == null)
			{
				_logger.LogError("Comando nulo recebido");
				return;
			}

			if (command.usuarioId <= 0)
			{
				_logger.LogError("Comando inválido recebido: usuário inválido");
				return;
			}

			if (command.Produtos == null || command.Produtos.Length == 0)
			{
				_logger.LogError("Comando inválido recebido: Ordem do usuário {usuario} não possui produtos", command.usuarioId);
				return;
			}

			try
			{
				var redis = _redisConnection.GetDatabase();

				var resultado = 0;

				foreach (var produto in command.Produtos)
				{
					var chaves = new RedisKey[] { $"quantidade-disponivel:{produto.id}", $"quantidade-reservada:{produto.id}" };

					RedisValue disponivelSeed = 0;
					RedisValue reservadaSeed = 0;

					if (await redis.KeyExistsAsync(chaves) < chaves.Length)
					{
						var registro = await GetInventarioAsync(produto.id);
						disponivelSeed = registro.quantidade_disponivel;
						reservadaSeed = registro.quantidade_reservada;
					}

					resultado = (int)await redis.ScriptEvaluateAsync(ValidaEstoqueScript, chaves, new RedisValue[] { produto.quantidadeSolicitada, disponivelSeed, reservadaSeed });

					if (resultado == 0)
					{
						await _bus.PublishAsync(new OrdemCanceladaSemAlteracaoCommand(command.ordemId, "Estoque insuficiente"));
						ReservasProcessadas.WithLabels("estoque_insuficiente").Inc();
						break;
					}
				}

				if (resultado == 1)
				{
					await _bus.PublishAsync(new ReservaInventarioDbSaveCommand(command.ordemId, command.usuarioId, command.Produtos));

					ReservasProcessadas.WithLabels("sucesso").Inc();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao processar o comando de reserva de inventário para a ordem {ordemId} do usuário {usuarioId}", command.ordemId, command.usuarioId);

				ReservasProcessadas.WithLabels("erro").Inc();

				throw;
			}
		}

		private async Task<Inventario> GetInventarioAsync(Guid produtoId)
		{
			if (RegistroInventario != null && RegistroInventario.produto_id == produtoId)
			{
				return RegistroInventario;
			}

			var registro = await _inventarioRepository.SelectByIdProduto(produtoId);

			if (registro == null)
			{
				throw new Exception("Registro de inventário não encontrado na base de dados");
			}

			RegistroInventario = registro;

			return registro;
		}
	}
}
