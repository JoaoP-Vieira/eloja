using eloja.Data.Entities;
using eloja.Data.Interface;
using eloja.Services.DTO;
using Microsoft.Extensions.Logging;

namespace eloja.Services.Queries
{
	public record ProdutosPaginadosQuery(string? Nome, Guid? CategoriaId, int? Pagina, int? TamanhoPagina, string? Direcao);

	public class ProdutosPaginadosQueryHandler(IProdutoRepository _produtoRepository, ILogger<ProdutosPaginadosQueryHandler> _logger)
	{
		private const int PAGINA_PADRAO = 1;
		private const int TAMANHO_PAGINA_PADRAO = 10;
		private const int TAMANHO_PAGINA_MAXIMO = 100;

		public async Task<PaginaDTO<ProdutoListaItemDTO>> Handle(ProdutosPaginadosQuery request, CancellationToken ct = default)
		{
			var pagina = request.Pagina ?? PAGINA_PADRAO;
			var tamanhoPagina = request.TamanhoPagina ?? TAMANHO_PAGINA_PADRAO;

			if (pagina < 1)
				throw new ArgumentException("pagina deve ser maior ou igual a 1.");

			if (tamanhoPagina < 1 || tamanhoPagina > TAMANHO_PAGINA_MAXIMO)
				throw new ArgumentException($"tamanhoPagina deve estar entre 1 e {TAMANHO_PAGINA_MAXIMO}.");

			var descendente = ParseDirecao(request.Direcao);

			var nome = string.IsNullOrWhiteSpace(request.Nome) ? null : request.Nome.Trim();

			var filtro = new FiltroProdutoListagem(nome, request.CategoriaId, pagina, tamanhoPagina, descendente);

			var totalItens = await _produtoRepository.CountAsync(filtro);
			var produtos = await _produtoRepository.SelectPaginadoAsync(filtro);

			var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

			_logger.LogInformation(
				"Listagem de produtos: pagina {Pagina}, tamanho {TamanhoPagina}, categoria {CategoriaId}, descendente {Descendente}, total {TotalItens}",
				pagina, tamanhoPagina, request.CategoriaId, descendente, totalItens);

			return new PaginaDTO<ProdutoListaItemDTO>
			{
				Itens = produtos.Select(ProdutoListaItemDTO.FromListagem).ToArray(),
				Pagina = pagina,
				TamanhoPagina = tamanhoPagina,
				TotalItens = totalItens,
				TotalPaginas = totalPaginas
			};
		}

		private static bool ParseDirecao(string? direcao)
		{
			if (string.IsNullOrWhiteSpace(direcao) || direcao.Equals("asc", StringComparison.OrdinalIgnoreCase))
				return false;

			if (direcao.Equals("desc", StringComparison.OrdinalIgnoreCase))
				return true;

			throw new ArgumentException("direcao deve ser 'asc' ou 'desc'.");
		}
	}
}
