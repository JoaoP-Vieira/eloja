using MongoDB.Bson.Serialization.Attributes;

namespace eloja_ordem_worker.Model
{
	public class Produto
	{
		[BsonElement("id")]
		public string Id { get; set; }

		[BsonElement("quantidade_solicitada")]
		public int QuantidadeSolicitada { get; set; }

		public Produto(string id, int quantidadeSolicitada)
		{
			Id = id;
			QuantidadeSolicitada = quantidadeSolicitada;
		}
	}
}
