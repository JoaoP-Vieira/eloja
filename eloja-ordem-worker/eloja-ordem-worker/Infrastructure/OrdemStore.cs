using eloja_ordem_worker.Model;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace eloja_ordem_worker.Infrastructure
{
	public class OrdemStore : IOrdemStore
	{
		private readonly IMongoCollection<Ordem> _ordensCollection;

		public OrdemStore()
		{
			var mongoClient = new MongoClient(Environment.GetEnvironmentVariable("MONGO_CONN_STR") ?? VariableNotSet("MONGO_CONN_STR")); 
			var mongoDatabase = mongoClient.GetDatabase(
				Environment.GetEnvironmentVariable("MONGO_DB") ?? VariableNotSet("MONGO_DB"));

			_ordensCollection = mongoDatabase.GetCollection<Ordem>(
				Environment.GetEnvironmentVariable("MONGO_COLLECTION") ?? VariableNotSet("MONGO_COLLECTION"));
		}

		private string VariableNotSet(string variableName)
		{
			throw new InvalidOperationException($"{variableName} environment variable is not set");
		}

		public async Task CreateAsync(Ordem ordem)
		{
			await _ordensCollection.InsertOneAsync(ordem);
		}

		public async Task<Ordem?> GetAsync(string id)
		{
			return await _ordensCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
		}

		public async Task<List<Ordem>> GetByUsuarioIdAsync(int usuarioId)
		{
			return await _ordensCollection.Find(x => x.UsuarioId == usuarioId).ToListAsync();
		}

		public async Task UpdateAsync(Ordem ordem)
		{
			await _ordensCollection.ReplaceOneAsync(x => x.Id == ordem.Id, ordem);
		}
	}
}
