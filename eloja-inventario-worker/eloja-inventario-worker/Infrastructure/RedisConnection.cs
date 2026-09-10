using StackExchange.Redis;
using eloja_inventario_worker.Configuration;
using Microsoft.Extensions.Options;

namespace eloja_inventario_worker.Infrastructure
{
	public class RedisConnection : IRedisConnection, IAsyncDisposable
	{
		private readonly IConnectionMultiplexer _muxer;
		private readonly ILogger<RedisConnection> _logger;

		public RedisConnection(IOptions<RedisSettings> settings, ILogger<RedisConnection> logger)
		{
			_logger = logger;

			var options = ConfigurationOptions.Parse($"{settings.Value.Host}:{settings.Value.Port}");
			if (!string.IsNullOrWhiteSpace(settings.Value.Password))
			{
				options.Password = settings.Value.Password;
			}

			try
			{
				_muxer = ConnectionMultiplexer.Connect(options);
				_logger.LogInformation("Redis connection established successfully at {Host}:{Port}", settings.Value.Host, settings.Value.Port);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to establish Redis connection");
				throw;
			}
		}

		public IDatabase GetDatabase(int db = 0)
		{
			return _muxer.GetDatabase(db);
		}

		public async ValueTask DisposeAsync()
		{
			if (_muxer != null)
			{
				try
				{
					await _muxer.CloseAsync();
					_logger.LogInformation("Redis connection closed");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error closing Redis connection");
				}
			}
		}
	}
}
