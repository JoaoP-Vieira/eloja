using eloja.Data.Interface;
using Wolverine;

namespace eloja.Services.Commands
{
	public record CreateOrdemMessage(Guid OrdemId, int UsuarioId, OrdemProduto[] Produtos);
	public record CreateOrdemBody(Dictionary<Guid, int> pedido);
	public record CreateOrdemCommand(int usuarioId, Dictionary<Guid, int> pedido);

	public class CreateOrdemCommandHandler(IProdutoRepository _produtoRepository, IMessageBus _bus)
	{
		public async Task Handle(CreateOrdemCommand request, CancellationToken ct = default)
		{
			foreach (var item in request.pedido.Keys)
			{
				if (request.pedido[item] <= 0)
					throw new ArgumentException("Quantidade inválida");

				if (!await _produtoRepository.ExistsByIdAsync(item))
					throw new KeyNotFoundException($"Produto {item} não encontrado");
			}

			await _bus.PublishAsync(
				new CreateOrdemMessage(Guid.NewGuid(), request.usuarioId, request.pedido.Select(x => new OrdemProduto {
					id = x.Key,
					quantidadeSolicitada = x.Value
				}).ToArray()));
		}
	}

	public class OrdemProduto
	{
		public Guid id { get; set; }
		public int quantidadeSolicitada { get; set; }
	}
}
