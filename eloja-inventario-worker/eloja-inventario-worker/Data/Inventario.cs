namespace eloja_inventario_worker.Data
{
	public class Inventario
	{
		public Guid id { get; set; }
		public Guid produto_id { get; set; }
		public int quantidade_disponivel { get; set; }
		public int quantidade_reservada { get; set; }
		public DateTime criado_em { get; set; }
		public DateTime? modificado_em { get; set; }
	}
}
