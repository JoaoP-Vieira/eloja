using StackExchange.Redis;

namespace eloja_inventario_worker.Infrastructure
{
	public interface IRedisConnection
	{
		IDatabase GetDatabase(int db = 0);
	}
}
