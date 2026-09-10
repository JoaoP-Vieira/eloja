using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eloja_ordem_worker.Model
{
	public class Ordem
	{
		[BsonId]
		[BsonElement("id")]
		public string Id { get; set; }

		[BsonElement("usuario_id")]
		public int UsuarioId { get; set; }

		[BsonElement("status")]
		public OrdemStatus Status { get; set; }

		[BsonElement("motivo_cancelamento")]
		public string MotivoCancelamento { get; set; }

		[BsonElement("produtos")]
		public List<Produto> Produtos { get; set; }

		public Ordem(string id, int usuarioId, List<Produto> produtos)
		{
			Id = id;
			UsuarioId = usuarioId;
			Status = OrdemStatus.Criada;
			MotivoCancelamento = string.Empty;
			Produtos = produtos;
		}
	}

	public enum OrdemStatus
	{
		Criada = 0,
		Confirmada = 1,
		Concluida = 2,
		Cancelada = 9
	}
}
