using eloja.Data.Entities;
using eloja.Data.Interface;
using eloja.Services.DTO;
using Microsoft.Extensions.Logging;

namespace eloja.Services.Commands
{
	public record CreateProdutoCommand(string nome, string descricao, string? foto, Guid categoriaId, decimal precoUnitario);

	public class CreateProdutoCommandHandler
	{
		private readonly ILogger<CreateProdutoCommandHandler> _logger;
		private readonly IProdutoRepository _produtoRepository;
		private readonly ICategoriaRepository _categoriaRepository;

		public CreateProdutoCommandHandler(
			ILogger<CreateProdutoCommandHandler> logger,
			IProdutoRepository produtoRepository,
			ICategoriaRepository categoriaRepository)
		{
			_logger = logger;
			_produtoRepository = produtoRepository;
			_categoriaRepository = categoriaRepository;
		}

		public async Task<ProdutoDTO> Handle(CreateProdutoCommand request, CancellationToken ct = default)
		{
			try
			{
				var produto = new Produto(request.nome, request.descricao, request.foto, request.categoriaId, request.precoUnitario);

				var categoria = await _categoriaRepository.SelectByIdAsync(produto.CategoriaId);

				if (categoria == null)
				{
					_logger.LogWarning("Categoria não encontrada: {CategoriaId}", produto.CategoriaId);
					throw new ArgumentException($"Categoria não encontrada: {produto.CategoriaId}");
				}

				await _produtoRepository.InsertAsync(produto);

				_logger.LogInformation("Produto criado com sucesso: {ProdutoId}", produto.Id);

				return ProdutoDTO.FromEntity(produto, categoria);
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Falha ao criar produto: {Message}", ex.Message);
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Falha ao criar produto");
				throw;
			}
		}
	}
}
