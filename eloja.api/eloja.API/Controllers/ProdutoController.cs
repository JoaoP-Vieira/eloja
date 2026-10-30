using eloja.Services.Commands;
using eloja.Services.DTO;
using eloja.Services.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace eloja.API.Controllers
{
	[ApiController]
	[Route("produto")]
	public class ProdutoController : Controller
	{
		private readonly IMessageBus _bus;

		public ProdutoController(IMessageBus bus)
		{
			_bus = bus;
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetProdutos(
			[FromQuery] string? nome,
			[FromQuery] Guid? categoriaId,
			[FromQuery] int? pagina,
			[FromQuery] int? tamanhoPagina,
			[FromQuery] string? direcao)
		{
			try
			{
				var resultado = await _bus.InvokeAsync<PaginaDTO<ProdutoListaItemDTO>>(
					new ProdutosPaginadosQuery(nome, categoriaId, pagina, tamanhoPagina, direcao));

				return Ok(resultado);
			}
			catch (ArgumentException ex)
			{
				return StatusCode(400, $"Erro ao listar produtos: {ex.Message}");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Erro ao listar produtos: {ex.Message}");
			}
		}

		[Authorize]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetProdutoById(Guid id)
		{
			try
			{
				var produto = await _bus.InvokeAsync<ProdutoDTO>(new ProdutoByIdQuery(id));
				
				return Ok(produto);
			}
			catch (KeyNotFoundException)
			{
				return NotFound($"Produto com ID {id} não encontrado.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Erro ao buscar produto: {ex.Message}");
			}
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> CreateProduto([FromBody] CreateProdutoCommand command)
		{
			try
			{
				var produto = await _bus.InvokeAsync<ProdutoDTO>(command);
				return Ok(produto);
			}
			catch (ArgumentException ex)
			{
				return StatusCode(400, $"Erro ao criar produto: {ex.Message}");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Erro ao criar produto: {ex.Message}");
			}
		}
	}
}
