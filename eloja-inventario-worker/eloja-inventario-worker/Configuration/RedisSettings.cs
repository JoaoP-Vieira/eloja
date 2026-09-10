namespace eloja_inventario_worker.Configuration
{
	public class RedisSettings
	{
		public string Host { get; set; } = string.Empty;
		public int Port { get; set; } = 6379;
		public string Password { get; set; } = string.Empty;
	}
}
