namespace eloja_inventario_worker.Model
{
	public class Produto
	{
		public Guid id { get; set; }
		public int quantidadeSolicitada { get; set; }
	}

	public class ProdutoRollback : Produto
	{
		public bool Rollback { get; set; }
	}
}
